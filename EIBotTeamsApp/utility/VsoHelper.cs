using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.Teams.TemplateBotCSharp;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using WorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace Microsoft.Office.EIBot.Service.utility
{
    public static class VsoHelper
    {
        public static readonly string Uri = ConfigurationManager.AppSettings["VsoOrgUrl"];
        public static readonly string Project = ConfigurationManager.AppSettings["VsoProject"];
        public static readonly string ResearchTaskType = "Research";
        public static readonly string VirtualAssistanceTaskType = "VirtualAssistance";
        public static readonly string[] TaskTypes = {ResearchTaskType, VirtualAssistanceTaskType};

        private static bool IsSupportedTask(string taskType) => TaskTypes.Any(taskType.Contains);

        public static WorkItemTrackingHttpClient GetWorkItemTrackingHttpClient()
        {
            try
            {
                Trace.TraceInformation($"Vso username is {ConfigurationManager.AppSettings["VsoUsername"]}");
                VssConnection connection = new VssConnection(new Uri(Uri), new VssAadCredential(
                    ConfigurationManager.AppSettings["VsoUsername"],
                    ConfigurationManager.AppSettings["VsoPassword"]));
                return connection.GetClient<WorkItemTrackingHttpClient>();
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"function", "GetWorkItemTrackingHttpClient" }
                });

                throw;
            }
        }

        /// <summary>
        /// Create a research task in VSO
        /// </summary>
        /// <param name="description"></param>
        /// <returns>Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem</returns>    
        public static async Task<int> CreateTaskInVso(
            string taskType,
            string requestedBy,
            string description,
            string assignedTo,
            DateTime targetDate,
            string teamsConversationId)
        {
            if (!IsSupportedTask(taskType))
            {
                throw new ArgumentException($"Vso Task type must be {ResearchTaskType} or {VirtualAssistanceTaskType}. " +
                                            $"Provided value = {taskType}");
            }
            JsonPatchDocument patchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Title",
                    Value = $"Request from {requestedBy} @ {DateTime.Now}"
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path = "/fields/System.Description", Value = description
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + GetRequestedByFieldNameBasedOnTaskType(taskType),
                    Value = requestedBy
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path = "/fields/Custom.AgentConversationId", Value = teamsConversationId
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path = "/fields/Custom.Freelancerplatform", Value = "UpWork"
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Custom.FreelancerPlatformJobId",
                    Value = "not assigned"
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.AssignedTo",
                    Value = assignedTo
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Scheduling.TargetDate",
                    Value = targetDate
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Custom.FreelancerName",
                    Value = "not assigned"
                }
            };

            try
            {
                using (WorkItemTrackingHttpClient workItemTrackingHttpClient = GetWorkItemTrackingHttpClient())
                {
                    var result = await workItemTrackingHttpClient.CreateWorkItemAsync(patchDocument, Project, "Research");
                    Trace.TraceInformation(@"Task Successfully Created: Research task #{0}", result.Id);

                    return (int)result.Id;
                }
            }
            catch (AggregateException ex)
            {
                WebApiConfig.TelemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    {"function", "CreateResearchTaskInVso" },
                    {"description", description },
                    {"requestedBy", requestedBy },
                    {"targetDate", targetDate.ToString() },
                });
                throw;
            }
        }

        private static string GetRequestedByFieldNameBasedOnTaskType(string taskType) => taskType == ResearchTaskType
            ? "Custom.RequestedBy"
            : taskType == VirtualAssistanceTaskType
                ? "Custom.RequestedByPhoneNo"
                : "not-set";

        public static async Task<int> AddTeamsAgentConversationId(
            int researchVsoId,
            string teamsConversationId)
        {
            Uri uri = new Uri(Uri);
            string project = Project;

            JsonPatchDocument patchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path = "/fields/Custom.TeamsConversationId", Value = teamsConversationId
                },
            };

            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = GetWorkItemTrackingHttpClient())
            {
                try
                {
                    WorkItem result =
                        await workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, researchVsoId);

                    Trace.TraceInformation(@"Bug Successfully Created: Research task #{0}", result.Id);

                    return (int)result.Id;
                }
                catch (AggregateException ex)
                {
                    Trace.TraceError(@"Error creating research task: {0}", ex.InnerException.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Execute a WIQL query to return a list of bugs using the .NET client library
        /// </summary>
        /// <returns>List of Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem</returns>
        public static async Task<List<WorkItem>> GetWorkItemsForUser(string taskType, string fromName)
        {
            //create a wiql object and build our query
            Wiql wiql = new Wiql()
            {
                Query = "Select [State], [Title], [Description], [Microsoft.VSTS.Scheduling.TargetDate], " +
                        "[Custom.EndUserConversationId], " +
                        "[Custom.AgentConversationId], " +
                        "[Custom.EndUserId], " +
                        "[Custom.EndUserName], " +
                        "[Custom.RequestedBy] " +
                        "From WorkItems " +
                        $"Where [Work Item Type] = '{taskType}' " +
                        "And [System.TeamProject] = '" + Project + "' " +
                        "And [Custom.RequestedBy] = '" + fromName + "' " +
                        "And [System.State] <> 'Closed' " +
                        "Order By [State] Asc, [Changed Date] Desc"
            };

            try
            {
                using (WorkItemTrackingHttpClient workItemTrackingHttpClient = GetWorkItemTrackingHttpClient())
                {
                    //execute the query to get the list of work items in the results
                    WorkItemQueryResult workItemQueryResult = await workItemTrackingHttpClient.QueryByWiqlAsync(wiql);

                    //some error handling                
                    if (workItemQueryResult.WorkItems.Count() != 0)
                    {
                        //need to get the list of our work item ids and put them into an array
                        List<int> list = new List<int>();
                        foreach (var item in workItemQueryResult.WorkItems)
                        {
                            list.Add(item.Id);
                        }
                        int[] arr = list.ToArray();

                        //build a list of the fields we want to see
                        string[] fields = new string[10];
                        fields[0] = "System.Id";
                        fields[1] = "System.Title";
                        fields[2] = "System.State";
                        fields[3] = "System.Description";
                        fields[4] = "Microsoft.VSTS.Scheduling.TargetDate";
                        fields[5] = "Custom.EndUserConversationId";
                        fields[6] = "Custom.AgentConversationId";
                        fields[7] = "Custom.EndUserId";
                        fields[8] = "Custom.EndUserName";
                        fields[9] = GetRequestedByFieldNameBasedOnTaskType(taskType);

                        //get work items for the ids found in query
                        List<WorkItem> workItems = workItemTrackingHttpClient.GetWorkItemsAsync(arr, fields, workItemQueryResult.AsOf).Result;

                        Trace.TraceInformation($"Query Results: {workItems.Count} items found");

                        //loop though work items and write to console
                        foreach (var workItem in workItems)
                        {
                            Trace.TraceInformation("{0}          {1}                     {2}", workItem.Id, workItem.Fields["System.Title"], workItem.Fields["System.State"]);
                        }

                        return workItems;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"function", "GetWorkItemsForUser" },
                    {"fromName", fromName }
                });

                throw;
            }
        }

        /// <summary>
        /// Execute a WIQL query to return a list of bugs using the .NET client library
        /// </summary>
        /// <returns>List of Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem</returns>
        public static async Task<string> GetProjectStatus(int vsoId)
        {
            try
            {
                using (WorkItemTrackingHttpClient workItemTrackingHttpClient = GetWorkItemTrackingHttpClient())
                {
                    WorkItem workitem = await workItemTrackingHttpClient.GetWorkItemAsync(vsoId);
                    return workitem.Fields["System.State"].ToString();
                }
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"function", "GetProjectStatus" },
                    {"vsoId", vsoId.ToString() }
                });

                throw;
            }
        }

        /// <summary>
        /// Execute a WIQL query to return a list of bugs using the .NET client library
        /// </summary>
        /// <returns>List of Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem</returns>
        public static async Task<EndUserAndAgentConversationMappingState>
            GetStateFromVsoGivenAgentConversationId(string agentConversationId)
        {
            //create a wiql object and build our query
            Wiql wiql = new Wiql()
            {
                Query = "Select " +
                        "[Custom.EndUserConversationId], " +
                        "[Custom.AgentConversationId], " +
                        "[Custom.EndUserId], " +
                        "[Custom.EndUserName] " +
                        "From WorkItems " +
                        $"Where " +
                        //$"[Work Item Type] = '{taskType}' And " +
                        "[System.TeamProject] = '" + Project + "' " +
                        "And [Custom.AgentConversationId] = '" + agentConversationId + "' "
            };

            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = GetWorkItemTrackingHttpClient())
            {
                //execute the query to get the list of work items in the results
                try
                {
                    WorkItemQueryResult workItemQueryResult = await workItemTrackingHttpClient.QueryByWiqlAsync(wiql);

                    //some error handling                
                    if (workItemQueryResult.WorkItems.Count() != 0)
                    {
                        //need to get the list of our work item ids and put them into an array
                        List<int> list = new List<int>();
                        foreach (var item in workItemQueryResult.WorkItems)
                        {
                            list.Add(item.Id);
                        }
                        int[] arr = list.ToArray();

                        //build a list of the fields we want to see
                        string[] fields = new string[4];
                        fields[0] = "Custom.EndUserConversationId";
                        fields[1] = "Custom.AgentConversationId";
                        fields[2] = "Custom.EndUserId";
                        fields[3] = "Custom.EndUserName";

                        //get work items for the ids found in query
                        List<WorkItem> workItems = await workItemTrackingHttpClient.GetWorkItemsAsync(arr, fields, workItemQueryResult.AsOf);

                        Trace.TraceInformation($"Query Results: {workItems.Count} items found");

                        //loop though work items and write to console
                        var firstWorkItem = workItems.FirstOrDefault();
                        if (firstWorkItem != null)
                        {
                            return new EndUserAndAgentConversationMappingState(
                                firstWorkItem.Id.ToString(),
                                firstWorkItem.Fields["Custom.EndUserName"].ToString(),
                                firstWorkItem.Fields["Custom.EndUserId"].ToString(),
                                firstWorkItem.Fields["Custom.EndUserConversationId"].ToString(),
                                firstWorkItem.Fields["Custom.AgentConversationId"].ToString()
                            );
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception e)
                {
                    WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                    {
                        {"function", "GetStateFromVsoGivenAgentConversationId" },
                        {"agentConversationId", agentConversationId }
                    });
                    throw;
                }
            }
        }

        public static async Task CloseProject(int vsoId)
        {
            JsonPatchDocument patchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path = "/fields/System.State", Value = "Closed"
                },
            };

            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = GetWorkItemTrackingHttpClient())
            {
                try
                {
                    WorkItem result =
                        await workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, vsoId);

                    Trace.TraceInformation($"Task successfully closed: Research task {result.Id}");
                }
                catch (AggregateException ex)
                {
                    WebApiConfig.TelemetryClient.TrackException(ex, new Dictionary<string, string>
                    {
                        {"function", "CloseProject" },
                        {"vsoId", vsoId.ToString() }
                    });

                    throw;
                }
            }
        }

        public static async Task<string> GetProjectSummary(int vsoId)
        {
            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = GetWorkItemTrackingHttpClient())
            {
                try
                {
                    WorkItem workitem = await workItemTrackingHttpClient.GetWorkItemAsync(vsoId);

                    string projectStatus = $"<b>Description</b>: {workitem.Fields["System.Description"]}\n\n" + 
                                           $"<b>Assigned to</b>: {workitem.Fields["System.AssignedTo"]}\n\n" + 
                                           $"<b>Due on</b>: {workitem.Fields["Microsoft.VSTS.Scheduling.TargetDate"]}\n\n" + 
                                           $"<b>Current State</b>: {workitem.Fields["System.State"]}\n\n";

                    Trace.TraceInformation($"Task successfully fetched task {workitem.Id}");

                    return projectStatus;
                }
                catch (AggregateException ex)
                {
                    WebApiConfig.TelemetryClient.TrackException(ex, new Dictionary<string, string>
                    {
                        {"function", "GetProjectSummary" },
                        {"vsoId", vsoId.ToString() }
                    });

                    throw;
                }
            }
        }

        public static async Task<WorkItem> GetWorkItem(int vsoId)
        {
            try
            {
                using (WorkItemTrackingHttpClient workItemTrackingHttpClient = GetWorkItemTrackingHttpClient())
                {
                    return await workItemTrackingHttpClient.GetWorkItemAsync(vsoId);
                }
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"function", "GetWorkItem" },
                    {"vsoId", vsoId.ToString() }
                });

                throw;
            }
        }
    }
}