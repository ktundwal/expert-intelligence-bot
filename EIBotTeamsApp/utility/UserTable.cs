using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Office.EIBot.Service.dialogs.EndUser;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.Teams.TemplateBotCSharp;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SnowMaker;

namespace Microsoft.Office.EIBot.Service.utility
{
    public class BotUser
    {
        public long Id { get; private set; }
        public string  Alias { get; private set; }
        public string Name { get; private set; }
        public string MobilePhone { get; private set; }

        public BotUser(long id, string alias, string name, string mobilePhone)
        {
            Id = id;
            Alias = alias;
            Name = name;
            MobilePhone = mobilePhone; 
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
            public UserEntity(long uniqueId, string alias, string name, string mobilePhone)
            {
                PartitionKey = uniqueId.ToString();
                RowKey = "";
                Alias = alias;
                Name = name;
                MobilePhone = mobilePhone;
            }

            public UserEntity() { }

            public string Alias { get; set; }
            public string Name { get; set; }
            public string MobilePhone { get; set; }
        }

        public async Task<BotUser> AddUser(string alias, string name, string mobilePhone)
        {
            var properties = new Dictionary<string, string>
            {
                {"class", "UserTable" },
                {"function", "AddUser" },
                {"name", name },
                {"alias", alias },
                {"mobilePhone", mobilePhone },
            };

            try
            {
                // Create the IdTableClient if it doesn't exist.
                await UserTableClient.CreateIfNotExistsAsync();

                var uniqueId = _uniqueIdGenerator.NextId($"{alias}-{name}-{mobilePhone}");
                var userIdentity = new UserEntity(uniqueId, alias, name, mobilePhone);

                // Execute the insert operation.
                await _retryPolicy.ExecuteAsync(async() => await UserTableClient.ExecuteAsync(TableOperation.Insert(userIdentity)));

                properties.Add("uniqueId", uniqueId.ToString());
                WebApiConfig.TelemetryClient.TrackEvent("AddUser", properties);

                return new BotUser(uniqueId, alias, name, mobilePhone);
            }
            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);
                throw;
            }
        }

        public async Task<IEnumerable<BotUser>> GetUserByMobilePhone(string mobilePhone) => await GetUserByColumn("MobilePhone", mobilePhone);

        public async Task<IEnumerable<BotUser>> GetUserById(string id) => await GetUserByColumn("PartitionKey", id);
        public async Task<IEnumerable<BotUser>> GetUserByName(string name) => await GetUserByColumn("Name", name);
        public async Task<IEnumerable<BotUser>> GetUserByAlias(string alias) => await GetUserByColumn("Alias", alias);

        private async Task<IEnumerable<BotUser>> GetUserByColumn(string columnName, string textToSearch)
        {
            var properties = new Dictionary<string, string>
            {
                {"class", "UserTable" },
                {"function", "GetUserByMobilePhone" },
                {"columnName", columnName },
                {"textToSearch", textToSearch },
            };

            try
            {
                // Construct the query operation for all customer entities where PartitionKey="Smith".
                TableQuery<UserEntity> rangeQuery = new TableQuery<UserEntity>()
                    .Where(TableQuery.GenerateFilterCondition(columnName, QueryComparisons.Equal, textToSearch));

                // Execute the retrieve operation.
                var queryResults = await _retryPolicy.ExecuteAsync(async () => await UserTableClient.ExecuteQueryAsync(rangeQuery));

                if (queryResults != null)
                {
                    properties.Add("numUsers", queryResults.Count.ToString());
                    WebApiConfig.TelemetryClient.TrackEvent("GetUserByMobilePhone", properties);
                    return queryResults.Select(r => new BotUser(long.Parse(r.PartitionKey), r.Alias, r.Name, r.MobilePhone));
                }

                return new List<BotUser>();
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);
                throw;
            }
        }
    }
}