using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Office.EIBot.Service.dialogs.EndUser;
using Microsoft.Teams.TemplateBotCSharp;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Office.EIBot.Service.utility
{
    // member: botName = BotId
    // channel: AgentChannelName = AgentChannelId
    public static class IdTable
    {
        private const string IdTableName = "idtable";
        private const string BotMemberType = "botmember";
        private const string ChannelType = "channel";
        public const string BotMember = "bot";
        public const string AgentResearchChannel = "agentresearchchannel";
        public const string AgentVirtualAssistanceChannel = "agentvirtualassistancechannel";
        private static readonly CloudTable IdTableClient;

        static IdTable() {
            try
            {
                // Retrieve the storage account from the connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);

                // Create the IdTableClient client.
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                // Create the CloudTable object that represents the "people" IdTableClient.
                IdTableClient = tableClient.GetTableReference(IdTableName);
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

        public class IdEntity : TableEntity
        {
            public IdEntity(string idType, string name, string id)
            {
                PartitionKey = idType;
                RowKey = name;
                Id = id;
            }

            public IdEntity() { }

            public string Id { get; set; }
        }

        public static async Task SetBotId(ChannelAccount botAccount)
        {
            var properties = new Dictionary<string, string>
            {
                {"function", "SetBotId" },
                {"name", botAccount.Name },
                {"id", botAccount.Id },
                {"memberType", BotMemberType },
            };

            try
            {
                // Create the IdTableClient if it doesn't exist.
                await IdTableClient.CreateIfNotExistsAsync();

                var botIdIdentity = new IdEntity(BotMemberType, botAccount.Name, botAccount.Id);

                // Create the TableOperation object that insert or replace the online status.
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(botIdIdentity);

                // Execute the insert operation.
                await IdTableClient.ExecuteAsync(insertOrReplaceOperation);

                WebApiConfig.TelemetryClient.TrackEvent("SetBotId", properties);
            }
            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);
            }
        }

        public static async Task<ChannelAccount> GetBotId()
        {
            var properties = new Dictionary<string, string>
            {
                {"function", "GetBotId" },
                {"memberType", BotMemberType },
            };

            ChannelAccount account = null;

            try
            {
                // Construct the query operation for all customer entities where PartitionKey="Smith".
                TableQuery<IdEntity> rangeQuery = new TableQuery<IdEntity>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, BotMemberType));

                // Execute the retrieve operation.
                var queryResults = await IdTableClient.ExecuteQueryAsync(rangeQuery);

                if (queryResults != null)
                {
                    account = queryResults.Select(r => new ChannelAccount(r.RowKey, r.Id)).FirstOrDefault();
                }

                properties.Add("botId", account != null ? account.Id : "not set");
                properties.Add("botName", account != null ? account.Name : "not set");

                WebApiConfig.TelemetryClient.TrackEvent("OnlineStatus", properties);
            }
            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);
            }
            return account;
        }

        public static async Task SetAgentChannel(string name, string id)
        {
            var properties = new Dictionary<string, string>
            {
                {"function", "SetAgentChannel" },
                {"name", name },
                {"id", id },
                {ChannelType, AgentResearchChannel },
            };

            try
            {
                // Create the IdTableClient if it doesn't exist.
                await IdTableClient.CreateIfNotExistsAsync();

                var entity = new IdEntity(AgentResearchChannel, name, id);

                // Create the TableOperation object that insert or replace the online status.
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);

                // Execute the insert operation.
                await IdTableClient.ExecuteAsync(insertOrReplaceOperation);

                WebApiConfig.TelemetryClient.TrackEvent("SetAgentChannel", properties);
            }
            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);
            }
        }
    }
}