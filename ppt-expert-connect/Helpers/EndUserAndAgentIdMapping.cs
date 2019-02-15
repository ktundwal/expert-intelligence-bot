using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
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

        public async Task<EndUserAndAgentIdMappingEntity> CreateNewMapping(string vsoId,
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

            var result = await _endUserAndAgentIdMappingClient.ExecuteAsync(insertOrReplaceOperation);
            return entity;
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

        public async Task SaveInVso(string vsoId, VsoHelper vso, EndUserAndAgentIdMappingEntity endUserAndAgentIdMappingEntity)
        {
            Uri uri = new Uri(vso.Uri);
            JsonPatchDocument patchDocument = new JsonPatchDocument()
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{VsoHelper.AgentConversationIdFieldName}",
                    Value = endUserAndAgentIdMappingEntity.AgentConversationId,
                },
                new JsonPatchOperation
                {
                    Operation = Operation.Add, Path = $"/fields/{VsoHelper.EndUserIdFieldName}", Value = endUserAndAgentIdMappingEntity.EndUserId,
                },
                new JsonPatchOperation
                {
                    Operation = Operation.Add, Path = $"/fields/{VsoHelper.EndUserNameFieldName}", Value = endUserAndAgentIdMappingEntity.EndUserName,
                },
            };

            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = vso.GetWorkItemTrackingHttpClient())
            {
                try
                {
                    Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem result =
                        await workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, Convert.ToInt32(vsoId));

                    Trace.TraceInformation($"Project {vsoId} successfully updated. {this}");
                }
                catch (Exception ex)
                {
//                    WebApiConfig.TelemetryClient.TrackException(ex, new Dictionary<string, string>
//                    {
//                        {"function", "SaveInVso" },
//                        {"debugNote", "Failed to update project" },
//                        {"vsoId", vsoId },
//                    });
                    Console.WriteLine("SaveInVso Failed to update project");
                    throw;
                }
            }
        }
    }

    public class EndUserModel
    {
        public string Name { get; set; }
        public string UserId { get; set; }
        public string Conversation { get; set; }
    }
}
