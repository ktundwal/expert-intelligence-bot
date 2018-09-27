using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Teams.TemplateBotCSharp;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    public static class OnlineStatus
    {
        private const string MemberOnlineStatusTableName = "MemberOnlineStatus";
        private const string MemberOnlineStatusTablePartitionKey = "onlinestatus";
        public const string AgentMemberType = "agent";
        public const string EndUserMemberType = "enduser";
        private static readonly CloudTable OnlineStatusTableClient;

        static OnlineStatus() {
            try
            {
                // Retrieve the storage account from the connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);

                // Create the OnlineStatusTableClient client.
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                // Create the CloudTable object that represents the "people" OnlineStatusTableClient.
                OnlineStatusTableClient = tableClient.GetTableReference(MemberOnlineStatusTableName);
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

        public class MemberOnlineStatusEntity : TableEntity
        {
            public MemberOnlineStatusEntity(string name, string botFrameWorkUserId, string memberType, DateTime lastActiveOn)
            {
                PartitionKey = MemberOnlineStatusTablePartitionKey;
                RowKey = memberType;
                Name = name;
                BotFrameWorkUserId = botFrameWorkUserId;
                LastActiveOn = lastActiveOn;
            }

            public MemberOnlineStatusEntity() { }

            public string Name { get; set; }

            public string BotFrameWorkUserId { get; set; }
            public DateTime LastActiveOn { get; set; }
        }

        public static async Task SetMemberActive(string memberName, string memberId, string memberType)
        {
            DateTime now = DateTime.UtcNow;
            var properties = new Dictionary<string, string>
            {
                {"function", "SetMemberActive" },
                {"name", memberName },
                {"id", memberId },
                {"memberType", memberType },
                {"timeStamp", now.ToString()},
            };

            try
            {
                // Create the OnlineStatusTableClient if it doesn't exist.
                await OnlineStatusTableClient.CreateIfNotExistsAsync();

                var memberOnlineStatusEntity = new MemberOnlineStatusEntity(
                    memberName,
                    memberId,
                    memberType,
                    now);

                // Create the TableOperation object that insert or replace the online status.
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(memberOnlineStatusEntity);

                // Execute the insert operation.
                await OnlineStatusTableClient.ExecuteAsync(insertOrReplaceOperation);

                WebApiConfig.TelemetryClient.TrackEvent("OnlineStatus", properties);
            }
            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);
            }
        }

        public static async Task<DateTime?> GetTimeWhenMemberWasLastActive(string memberType)
        {
            var properties = new Dictionary<string, string>
            {
                {"function", "GetTimeWhenMemberWasLastActive" },
                {"memberType", memberType },
            };

            DateTime? timeStamp = null;

            try
            {
                // Create the OnlineStatusTableClient if it doesn't exist.
                await OnlineStatusTableClient.CreateIfNotExistsAsync();

                // Construct the query operation for all customer entities where PartitionKey="Smith".
                TableQuery<MemberOnlineStatusEntity> rangeQuery = new TableQuery<MemberOnlineStatusEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, MemberOnlineStatusTablePartitionKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, AgentMemberType)));

                // Execute the retrieve operation.
                var queryResults = await OnlineStatusTableClient.ExecuteQueryAsync(rangeQuery);

                if (queryResults != null)
                {
                    timeStamp = queryResults
                        .OrderByDescending(result => result.LastActiveOn)
                        .Select(r => r.LastActiveOn)
                        .FirstOrDefault();
                }

                properties.Add("timeStamp", timeStamp != null ? timeStamp.ToString() : "not set");

                WebApiConfig.TelemetryClient.TrackEvent("OnlineStatus", properties);
            }
            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);
            }
            return timeStamp;
        }

        public static async Task<IEnumerable<ChannelAccount>> GetAgentIds()
        {
            var properties = new Dictionary<string, string>
            {
                {"function", "GetAgentIds" },
            };

            try
            {
                // Create the OnlineStatusTableClient if it doesn't exist.
                await OnlineStatusTableClient.CreateIfNotExistsAsync();

                // Construct the query operation for all customer entities where PartitionKey="Smith".
                TableQuery<MemberOnlineStatusEntity> rangeQuery = new TableQuery<MemberOnlineStatusEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, MemberOnlineStatusTablePartitionKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, AgentMemberType)));

                // Execute the retrieve operation.
                var queryResults = await OnlineStatusTableClient.ExecuteQueryAsync(rangeQuery);

                if (queryResults != null)
                {
                    properties.Add("timeStamp", string.Join(" ", queryResults.Select(r => r.Name)));
                    return queryResults.Select(r => new ChannelAccount(r.BotFrameWorkUserId, r.Name));
                }

                WebApiConfig.TelemetryClient.TrackEvent("GetAgentIds", properties);
            }
            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);
            }
            return new List<ChannelAccount>();
        }
    }
}