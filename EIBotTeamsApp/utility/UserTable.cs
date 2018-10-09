using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Office.EIBot.Service.dialogs.EndUser;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.Teams.TemplateBotCSharp;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SnowMaker;

namespace Microsoft.Office.EIBot.Service.utility
{
    [Serializable]
    public class UserProfile
    {
        public long Id { get; private set; }
        public string  Alias { get; private set; }
        public string Name { get; private set; }
        public string MobilePhone { get; private set; }

        public UserProfile(long id, string alias, string name, string mobilePhone)
        {
            Id = id;
            Alias = alias;
            Name = name;
            MobilePhone = mobilePhone; 
        }

        public override string ToString()
        {
            return $"{Name} {Alias}: [MobilePhone: {MobilePhone}]";
        }
    }

    // member: botName = BotId
    // channel: AgentChannelName = AgentChannelId
    public class UserTable
    {
        private const string UserTableName = "usertable";
        private const string BotMemberType = "botmember";
        private const string ChannelType = "channel";
        public const string BotMember = "bot";
        public const string AgentResearchChannel = "agentresearchchannel";
        public const string AgentVirtualAssistanceChannel = "agentvirtualassistancechannel";
        private const string TeamsUserIdColumnName = "TeamsUserId";
        private const string SmsUserIdColumnName = "SmsUserId";
        private const string MobilePhoneColumnName = "MobilePhone";
        private const string PartitionKeyColumnName = "PartitionKey";
        private const string NameColumnName = "Name";
        private const string AliasColumnName = "Alias";
        private const string MsTeamChannelName = ActivityHelper.MsTeamChannelId;
        private readonly CloudTable UserTableClient;
        readonly UniqueIdGenerator _uniqueIdGenerator;
        readonly RetryPolicy _retryPolicy;

        public UserTable()
        {
            
            try
            {
                // Retrieve the storage account from the connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

                // Create the IdTableClient client.
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                // Create the CloudTable object that represents the "people" IdTableClient.
                UserTableClient = tableClient.GetTableReference(UserTableName);

                _uniqueIdGenerator = new UniqueIdGenerator(new BlobOptimisticDataStore(storageAccount, "UniqueIdGenerator"));

                _retryPolicy = BotConnectorUtility.BuildRetryPolicy();
            }
            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"debugNote", "failed to init OnlineStatus table client" },
                });
                throw;
            }
        }

        public class UserEntity : TableEntity
        {
            public UserEntity(long uniqueId, string alias, string name, string mobilePhone, string teamsUserId, string smsUserId)
            {
                PartitionKey = uniqueId.ToString();
                RowKey = "";
                Alias = alias;
                Name = name;
                MobilePhone = mobilePhone;
                TeamsUserId = teamsUserId;
                SmsUserId = smsUserId;
                CortanaUserId = "";
                EmailUserId = "";
            }

            public UserEntity() { }

            public string TeamsUserId { get; set; }
            public string SmsUserId { get; set; }
            public string CortanaUserId { get; set; }
            public string EmailUserId { get; set; }
            public string Alias { get; set; }
            public string Name { get; set; }
            public string MobilePhone { get; set; }
        }

        public async Task<UserProfile> AddUser(string channelId, string channelSpecificId, string name="", string mobilePhone="", string alias="")
        {
            var properties = new Dictionary<string, string>
            {
                {"class", "UserTable" },
                {"function", "AddUser" },
                {"channelId", channelId },
                {"channelSpecificId", channelSpecificId },
                {"name", name },
                {"mobilePhone", mobilePhone },
                {"alias", alias },
            };

            try
            {
                if (string.IsNullOrEmpty(channelId) || string.IsNullOrEmpty(channelSpecificId))
                {
                    throw new ArgumentNullException($"{nameof(channelId)} or {nameof(channelSpecificId)} cant be null");
                }

                // Create the IdTableClient if it doesn't exist.
                await UserTableClient.CreateIfNotExistsAsync();

                // check if we already have an account for given bot Id
                var users = await GetUserByChannelSpecificId(channelId, channelSpecificId);
                if (users.Any())
                {
                    if (users.Length > 1) throw new Exception("Found more than 1 user in store with same teams user id");

                    return users.First();
                }

                // add user
                var uniqueId = _uniqueIdGenerator.NextId($"{alias}-{name}-{mobilePhone}");
                var userIdentity = new UserEntity(uniqueId,
                    alias,
                    name,
                    mobilePhone,
                    channelId == ActivityHelper.MsTeamChannelId ? channelSpecificId : "",
                    channelId == ActivityHelper.SmsChannelId ? channelSpecificId : ""
                    );

                // Execute the insert operation.
                await _retryPolicy.ExecuteAsync(async() => await UserTableClient.ExecuteAsync(TableOperation.InsertOrReplace(userIdentity)));

                properties.Add("uniqueId", uniqueId.ToString());
                WebApiConfig.TelemetryClient.TrackEvent("AddOrGetTeamsUser", properties);

                return new UserProfile(uniqueId, alias, name, mobilePhone);
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);
                throw;
            }
        }

        public async Task<IEnumerable<UserProfile>> GetUserByTeamsUserId(string teamsUserId) => await GetUserByColumn(TeamsUserIdColumnName, teamsUserId);
        public async Task<IEnumerable<UserProfile>> GetUserByMobilePhone(string mobilePhone) => await GetUserByColumn(MobilePhoneColumnName, mobilePhone);

        public async Task<IEnumerable<UserProfile>> GetUserById(string id) => await GetUserByColumn(PartitionKeyColumnName, id);
        public async Task<IEnumerable<UserProfile>> GetUserByName(string name) => await GetUserByColumn(NameColumnName, name);
        public async Task<IEnumerable<UserProfile>> GetUserByAlias(string alias) => await GetUserByColumn(AliasColumnName, alias);

        private async Task<IEnumerable<UserProfile>> GetUserByColumn(string columnName, string textToSearch)
        {
            var properties = new Dictionary<string, string>
            {
                {"class", "UserTable" },
                {"function", "GetUserByColumn" },
                {"columnName", columnName },
                {"textToSearch", textToSearch },
            };

            try
            {
                // Create the IdTableClient if it doesn't exist.
                await UserTableClient.CreateIfNotExistsAsync();

                // Construct the query operation for all customer entities where PartitionKey="Smith".
                TableQuery<UserEntity> rangeQuery = new TableQuery<UserEntity>()
                    .Where(TableQuery.GenerateFilterCondition(columnName, QueryComparisons.Equal, textToSearch));

                // Execute the retrieve operation.
                var queryResults = await _retryPolicy.ExecuteAsync(async () => await UserTableClient.ExecuteQueryAsync(rangeQuery));

                if (queryResults != null)
                {
                    properties.Add("numUsers", queryResults.Count.ToString());
                    WebApiConfig.TelemetryClient.TrackEvent("GetUserByColumn", properties);
                    return queryResults.Select(r => new UserProfile(long.Parse(r.PartitionKey), r.Alias, r.Name, r.MobilePhone));
                }

                return new List<UserProfile>();
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);
                throw;
            }
        }

        public async Task<UserProfile[]> GetUserByChannelSpecificId(string channelId, string id)
        {
            string columnName;
            switch (channelId)
            {
                case ActivityHelper.SmsChannelId:
                    columnName = SmsUserIdColumnName;
                    break;
                case ActivityHelper.MsTeamChannelId:
                    columnName = TeamsUserIdColumnName;
                    break;
                default:
                    throw new Exception("Unsupported channel");
            }

            var users = await GetUserByColumn(columnName, id);
            return users.ToArray();
        }
    }
}