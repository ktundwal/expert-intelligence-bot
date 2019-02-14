using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.ExpertConnect.Helpers;
using Microsoft.ExpertConnect.Models;
using Microsoft.IdentityModel.Protocols;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace com.microsoft.ExpertConnect.Helpers
{
    public class VsoHelper
    {
        public const string EndUserConversationIdFieldName = "Custom.EndUserConversationId";
        public const string EndUserNameFieldName = "Custom.EndUserName";
        public const string EndUserIdFieldName = "Custom.EndUserId";
        public const string EndUserMobilePhoneFieldName = "Custom.MobilePhone";
        public const string EndUserAliasFieldName = "Custom.Alias";
        private const string StateFieldName = "System.State";
        private const string TitleFieldName = "System.Title";
        private const string FreelancerplatformFieldName = "Custom.Freelancerplatform";
        private const string FreelancerplatformJobIdFieldName = "Custom.FreelancerPlatformJobId";
        private const string RequestedByFieldName = "Custom.RequestedBy";
        public readonly string Uri;
        public readonly string Project;
        public static readonly string ResearchTaskType = "Research";
        public static readonly string VirtualAssistanceTaskType = "Virtual Assistance";
        public static readonly string[] TaskTypes = { ResearchTaskType, VirtualAssistanceTaskType };

        public static readonly string AgentConversationIdFieldName = "Custom.AgentConversationId";
        public static readonly string DescriptionFieldName = "System.Description";

        private static bool IsSupportedTask(string taskType) => TaskTypes.Any(taskType.Contains);

        private readonly AppSettings _appSettings;

        public VsoHelper(AppSettings appSettings)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

            Uri = appSettings.VsoOrgUrl;
            Project = appSettings.VsoProject;

        }

        public WorkItemTrackingHttpClient GetWorkItemTrackingHttpClient()
        {
            try
            {
                Trace.TraceInformation($"Vso username is {_appSettings.VsoUsername}");
                VssConnection connection = new VssConnection(new Uri(Uri), new VssAadCredential(
                    _appSettings.VsoUsername,
                    _appSettings.VsoPassword));
                return connection.GetClient<WorkItemTrackingHttpClient>();
            }
            catch (Exception e)
            {
                Console.WriteLine("GetWorkItemTrackingHttpClient");
//                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
//                {
//                    {"function", "GetWorkItemTrackingHttpClient" }
//                });

                throw;
            }
        }

        /// <summary>
        /// Create a research task in VSO
        /// </summary>
        /// <param name="description"></param>
        /// <returns>Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem</returns>
        public async Task<int> CreateTaskInVso(
            string taskType,
            string requestedBy,
            string description,
            string assignedTo,
            DateTime targetDate,
            string teamsConversationId,
//            UserProfile userProfile,
            string userProfileAlias,
            string channelId)
        {
            var properties = new Dictionary<string, string>
            {
                {"class", "VsoHelpers" },
                {"function", "CreateTaskInVso" },
                {"taskType", taskType },
                {"description", description },
                {"requestedBy", requestedBy },
                {"teamsConversationId", teamsConversationId },
                {"targetDate", targetDate.ToString() },
            };

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
                    Value = $"Web research request from {userProfileAlias} via {channelId} due {targetDate}"
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path = $"/fields/{DescriptionFieldName}", Value = description
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + GetRequestedByFieldNameBasedOnTaskType(taskType),
                    Value = requestedBy
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path = $"/fields/{EndUserAliasFieldName}", Value = userProfileAlias
                },
//                new JsonPatchOperation()
//                {
//                    Operation = Operation.Add, Path = $"/fields/{EndUserMobilePhoneFieldName}", Value = userProfile.MobilePhone
//                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path = $"/fields/{AgentConversationIdFieldName}", Value = teamsConversationId
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path = $"/fields/{FreelancerplatformFieldName}", Value = "UpWork"
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{FreelancerplatformJobIdFieldName}",
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
                    var result = await workItemTrackingHttpClient.CreateWorkItemAsync(patchDocument, Project, taskType);
                    Trace.TraceInformation($"Task Successfully Created: {taskType} task {result.Id}");

                    var taskInVso = (int)result.Id;

                    properties.Add("vsoId", taskInVso.ToString());
                    Console.WriteLine("CreateTaskInVso");
//                    WebApiConfig.TelemetryClient.TrackEvent("CreateTaskInVso", properties);

                    return taskInVso;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                //                WebApiConfig.TelemetryClient.TrackException(ex, properties);
                throw;
            }
        }

        private string GetRequestedByFieldNameBasedOnTaskType(string taskType) => taskType == ResearchTaskType
            ? RequestedByFieldName
            : taskType == VirtualAssistanceTaskType
                ? "Custom.RequestedByPhoneNo"
                : "not-set";

        public async Task<int> AddTeamsAgentConversationId(
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
                catch (Exception ex)
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
        public async Task<List<WorkItem>> GetWorkItemsForUser(string taskType, string channelId, string fromName)
        {
            //create a wiql object and build our query
            Wiql wiql = new Wiql()
            {
                Query = "Select [State], [Title], [Description], [Microsoft.VSTS.Scheduling.TargetDate], " +
                        $"[{EndUserConversationIdFieldName}], " +
                        $"[{AgentConversationIdFieldName}], " +
                        $"[{EndUserIdFieldName}], " +
                        $"[{EndUserNameFieldName}], " +
                        $"[{RequestedByFieldName}] " +
                        "From WorkItems " +
                        $"Where [Work Item Type] = '{taskType}' " +
                        $"And [System.TeamProject] = '{Project}' " +
                        $"And [{channelId == RequestedByFieldName}] = '{fromName}' " +
                        $"And [{StateFieldName}] <> 'Closed' " +
                        "Order By [State] Asc, [Changed Date] Desc",
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
                        fields[1] = TitleFieldName;
                        fields[2] = StateFieldName;
                        fields[3] = DescriptionFieldName;
                        fields[4] = "Microsoft.VSTS.Scheduling.TargetDate";
                        fields[5] = EndUserConversationIdFieldName;
                        fields[6] = AgentConversationIdFieldName;
                        fields[7] = EndUserIdFieldName;
                        fields[8] = EndUserNameFieldName;
                        fields[9] = GetRequestedByFieldNameBasedOnTaskType(taskType);

                        //get work items for the ids found in query
                        List<WorkItem> workItems = workItemTrackingHttpClient.GetWorkItemsAsync(arr, fields, workItemQueryResult.AsOf).Result;

                        Trace.TraceInformation($"Query Results: {workItems.Count} items found");

                        //loop though work items and write to console
                        foreach (var workItem in workItems)
                        {
                            Trace.TraceInformation("{0}          {1}                     {2}", workItem.Id, workItem.Fields[TitleFieldName], workItem.Fields[StateFieldName]);
                        }

                        return workItems;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
//                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
//                {
//                    {"function", "GetWorkItemsForUser" },
//                    {"fromName", fromName }
//                });

                throw;
            }
        }

        /// <summary>
        /// Execute a WIQL query to return a list of bugs using the .NET client library
        /// </summary>
        /// <returns>List of Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem</returns>
        public async Task<string> GetProjectStatus(int vsoId)
        {
            try
            {
                using (WorkItemTrackingHttpClient workItemTrackingHttpClient = GetWorkItemTrackingHttpClient())
                {
                    WorkItem workitem = await workItemTrackingHttpClient.GetWorkItemAsync(vsoId);
                    return workitem.Fields[StateFieldName].ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
//                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
//                {
//                    {"function", "GetProjectStatus" },
//                    {"vsoId", vsoId.ToString() }
//                });

                throw;
            }
        }

        /// <summary>
        /// Execute a WIQL query to return a list of bugs using the .NET client library
        /// </summary>
        /// <returns>List of Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem</returns>
        public async Task<EndUserAndAgentIdMapping.EndUserAndAgentIdMappingEntity>
            GetStateFromVsoGivenAgentConversationId(string agentConversationId)
        {
            //create a wiql object and build our query
            Wiql wiql = new Wiql()
            {
                Query = "Select " +
                        $"[{EndUserConversationIdFieldName}], " +
                        $"[{AgentConversationIdFieldName}], " +
                        $"[{EndUserIdFieldName}], " +
                        $"[{EndUserNameFieldName}] " +
                        "From WorkItems " +
                        $"Where " +
                        //$"[Work Item Type] = '{taskType}' And " +
                        "[System.TeamProject] = '" + Project + "' " +
                        $"And [{AgentConversationIdFieldName}] = '" + agentConversationId + "' "
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
                        fields[0] = EndUserConversationIdFieldName;
                        fields[1] = AgentConversationIdFieldName;
                        fields[2] = EndUserIdFieldName;
                        fields[3] = EndUserNameFieldName;

                        //get work items for the ids found in query
                        List<WorkItem> workItems = await workItemTrackingHttpClient.GetWorkItemsAsync(arr, fields, workItemQueryResult.AsOf);

                        Trace.TraceInformation($"Query Results: {workItems.Count} items found");

                        //loop though work items and write to console
                        var firstWorkItem = workItems.FirstOrDefault();
                        if (firstWorkItem != null)
                        {
                            return new EndUserAndAgentIdMapping.EndUserAndAgentIdMappingEntity(
                                firstWorkItem.Id.ToString(),
                                firstWorkItem.Fields[EndUserNameFieldName].ToString(),
                                firstWorkItem.Fields[EndUserIdFieldName].ToString(),
                                firstWorkItem.Fields[EndUserConversationIdFieldName].ToString(),
                                firstWorkItem.Fields[AgentConversationIdFieldName].ToString()
                            );
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception e)
                {
//                    WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
//                    {
//                        {"function", "GetStateFromVsoGivenAgentConversationId" },
//                        {"agentConversationId", agentConversationId }
//                    });
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        public async Task CloseProject(int vsoId)
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
                catch (Exception ex)
                {
//                    WebApiConfig.TelemetryClient.TrackException(ex, new Dictionary<string, string>
//                    {
//                        {"function", "CloseProject" },
//                        {"vsoId", vsoId.ToString() }
//                    });
                    Console.WriteLine(ex);

                    throw;
                }
            }
        }

        public async Task<string> GetProjectSummary(int vsoId)
        {
            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = GetWorkItemTrackingHttpClient())
            {
                try
                {
                    WorkItem workitem = await workItemTrackingHttpClient.GetWorkItemAsync(vsoId);

                    string projectStatus = $"<b>Description</b>: {workitem.Fields[DescriptionFieldName]}\n\n" +
                                           $"<b>Assigned to</b>: {workitem.Fields["System.AssignedTo"]}\n\n" +
                                           $"<b>Due on</b>: {workitem.Fields["Microsoft.VSTS.Scheduling.TargetDate"]}\n\n" +
                                           $"<b>Current State</b>: {workitem.Fields[StateFieldName]}\n\n";

                    Trace.TraceInformation($"Task successfully fetched task {workitem.Id}");

                    return projectStatus;
                }
                catch (Exception ex)
                {
//                    WebApiConfig.TelemetryClient.TrackException(ex, new Dictionary<string, string>
//                    {
//                        {"function", "GetProjectSummary" },
//                        {"vsoId", vsoId.ToString() }
//                    });
                    Console.WriteLine(ex);

                    throw;
                }
            }
        }

        public async Task<WorkItem> GetWorkItem(int vsoId)
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
//                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
//                {
//                    {"function", "GetWorkItem" },
//                    {"vsoId", vsoId.ToString() }
//                });
                Console.WriteLine(e);

                throw;
            }
        }

        public async Task<string> GetAgentConversationIdForVso(int vsoId)
        {
            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = GetWorkItemTrackingHttpClient())
            {
                try
                {
                    WorkItem workitem = await workItemTrackingHttpClient.GetWorkItemAsync(vsoId);

                    return (string)(workitem.Fields.TryGetValue(AgentConversationIdFieldName, out object conversationId)
                        ? conversationId
                        : "");
                }
                catch (Exception ex)
                {
                    //                    WebApiConfig.TelemetryClient.TrackException(ex, new Dictionary<string, string>
                    //                    {
                    //                        {"function", "GetAgentConversationIdForVso" },
                    //                        {"vsoId", vsoId.ToString() },
                    //                    });
                    Console.WriteLine(ex);
                    throw;
                }
            }
        }

        public async Task<int> CreateProject(ITurnContext context, UserInfo userInfo)
        {
            try
            {
                // TODO: Need to compile the UserChoices into one string
                var userChoices = userInfo.Introduction;

                // TODO: Need to obtain User Data
                var userProfile = context.Activity.From.Name; // TODO: Replace with user object
                var deadline = DateTime.UtcNow.AddHours(Convert.ToInt32(_appSettings.ResearchProjectViaTeamsMinHours));

                var vsoTicketNumber = await CreateTaskInVso(
                    ResearchTaskType,
                    context.Activity.From.Name,
                    userChoices,
                    _appSettings.AgentToAssignVsoTasksTo,
                    deadline,
                    string.Empty, // TODO: Why?
                    userProfile,
                    context.Activity.ChannelId);

                userInfo.VsoId = vsoTicketNumber.ToString();
                return vsoTicketNumber;
            }
            catch (System.Exception e)
            {
                //TODO: redo catch clause
                try
                {
                    // close this ticket
                    await CloseProject(Convert.ToInt32(userInfo.VsoId)); // Exception
                }
                catch (System.Exception exception)
                {
                    Console.WriteLine("Error closing project during exception received in CreateProject");
//                    WebApiConfig.TelemetryClient.TrackException(exception, new Dictionary<string, string>
//                    {
//                        {"debugNote", "Error closing project during exception received in CreateProject" },
//                        {"CreateProjectException", e.ToString() },
//                    });
                }

                return int.MinValue;
            }
        }

    }
}
