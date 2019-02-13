using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.ExpertConnect.Helpers
{
    public class EndUserAndAgentIdMapping
    {
        private const string EndUserAndAgentIdMappingTableName = "endUserAndAgentIdMappingTable";
        private readonly CloudTable _endUserAndAgentIdMappingClient;
        public const string VsoIdKey = "VsoId";
        public const string EndUserNameKey = "EndUserName";
        public const string EndUserIdKey = "EndUserId";
        public const string EndUserConversationIdKey = "EndUserConversationId";
        public const string AgentConversationIdKey = "AgentConversationId";

        public EndUserAndAgentIdMapping(string storageConnectionString)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                _endUserAndAgentIdMappingClient = tableClient.GetTableReference(EndUserAndAgentIdMappingTableName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        public class EndUserAndAgentIdMappingEntity : TableEntity
        {
            public EndUserAndAgentIdMappingEntity(
                string vsoId,
                string endUserName,
                string endUserId,
                string endUserConversationReference,
                string agentConversationId)
            {
                PartitionKey = vsoId;
                RowKey = vsoId;
                VsoId = vsoId;
                EndUserName = endUserName;
                EndUserId = endUserId;
                EndUserConversationReference = endUserConversationReference;
                AgentConversationId = agentConversationId;
            }

            public EndUserAndAgentIdMappingEntity() { }
            public string VsoId { get; set; }
            public string EndUserName { get; set; }
            public string EndUserId { get; set; }
            public string EndUserConversationReference { get; set; }
            public string AgentConversationId { get; set; }
        }

        public async Task CreateNewMapping(string vsoId,
            string endUserName,
            string endUserId,
            string endUserConversationReference,
            string agentConversationId)
        {
            // TODO: surround by try/catch
            await _endUserAndAgentIdMappingClient.CreateIfNotExistsAsync();
            var entity = new EndUserAndAgentIdMappingEntity(
                vsoId, endUserName, endUserId, endUserConversationReference, agentConversationId);
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);

            await _endUserAndAgentIdMappingClient.ExecuteAsync(insertOrReplaceOperation);
        }

        public async Task<string> GetAgentConversationId(string vsoId)
        {
            string returnVal = null;
            try
            {
                TableQuery<EndUserAndAgentIdMappingEntity> rangeQuery =
                    new TableQuery<EndUserAndAgentIdMappingEntity>()
                        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, vsoId));

                var queryResults = await _endUserAndAgentIdMappingClient.ExecuteQuerySegmentedAsync(rangeQuery, null);
                if (queryResults != null)
                {
                    returnVal = queryResults.Select(e => e.AgentConversationId).FirstOrDefault();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return returnVal;
        }

        public async Task<EndUserModel> GetEndUserInfo(string vsoId)
        {
            EndUserModel eUM = null;

            try
            {
                TableQuery<EndUserAndAgentIdMappingEntity> rangeQuery = 
                    new TableQuery<EndUserAndAgentIdMappingEntity>()
                        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, vsoId));

                var queryResults = await _endUserAndAgentIdMappingClient.ExecuteQuerySegmentedAsync(rangeQuery, null);
                if (queryResults != null)
                {
                    eUM = queryResults.Select(e => new EndUserModel()
                    {
                        Name = e.EndUserName,
                        UserId = e.EndUserId,
                        Conversation = e.EndUserConversationReference
                    }).FirstOrDefault();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return eUM;
        }

        public async Task<string> GetVsoTicketFromUserID(string endUserId)
        {
            string returnVal = null;
            try
            {
                TableQuery<EndUserAndAgentIdMappingEntity> rangeQuery =
                    new TableQuery<EndUserAndAgentIdMappingEntity>()
                        .Where(TableQuery.GenerateFilterCondition("EndUserId", QueryComparisons.Equal, endUserId));

                var queryResults = await _endUserAndAgentIdMappingClient.ExecuteQuerySegmentedAsync(rangeQuery, null);
                if (queryResults != null)
                {
                    // TODO: add functionality to deal with multiple projects
                    returnVal = queryResults.Select(e => e.VsoId).FirstOrDefault();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return returnVal;
        }
    }

    public class EndUserModel
    {
        public string Name { get; set; }
        public string UserId { get; set; }
        public string Conversation { get; set; }
    }
}
