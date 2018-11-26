using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.Teams.TemplateBotCSharp;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Microsoft.Office.EIBot.Service.utility
{
    public class EndUserAndAgentConversationMappingState
    {
        public const string VsoIdKey = "VsoId";
        public const string EndUserNameKey = "EndUserName";
        public const string EndUserIdKey = "EndUserId";
        public const string EndUserConversationIdKey = "EndUserConversationId";
        public const string AgentConversationIdKey = "AgentConversationId";

        public string VsoId { get; }
        public string EndUserName { get; }
        public string EndUserId { get; }
        public string EndUserConversationId { get; }
        public string AgentConversationId { get; }

        public bool IsConversationHandedOverToAgent =>
            !string.IsNullOrEmpty(EndUserConversationId) && !string.IsNullOrEmpty(AgentConversationId);

        public EndUserAndAgentConversationMappingState(
            string vsoId,
            string endUserName,
            string endUserId,
            string endUserConversationId,
            string agentConversationId)
        {
            VsoId = vsoId;
            EndUserName = endUserName;
            EndUserId = endUserId;
            EndUserConversationId = endUserConversationId;
            AgentConversationId = agentConversationId;
        }

        private void SetProperties(BotData botData)
        {
            botData.SetProperty(VsoIdKey, VsoId);
            botData.SetProperty(EndUserNameKey, EndUserName);
            botData.SetProperty(EndUserIdKey, EndUserId);
            botData.SetProperty(EndUserConversationIdKey, EndUserConversationId);
            botData.SetProperty(AgentConversationIdKey, AgentConversationId);
        }

        public static async Task<EndUserAndAgentConversationMappingState> GetFromVso(int vsoId)
        {
            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = VsoHelper.GetWorkItemTrackingHttpClient())
            {
                try
                {
                    TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem workitem =
                        await workItemTrackingHttpClient.GetWorkItemAsync(vsoId);

                    return new EndUserAndAgentConversationMappingState(vsoId.ToString(),
                        workitem.Fields[VsoHelper.EndUserNameFieldName].ToString(),
                        workitem.Fields[VsoHelper.EndUserIdFieldName].ToString(),
                        workitem.Fields[VsoHelper.EndUserConversationIdFieldName].ToString(),
                        workitem.Fields[VsoHelper.AgentConversationIdFieldName].ToString());
                }
                catch (Exception ex)
                {
                    WebApiConfig.TelemetryClient.TrackException(ex, new Dictionary<string, string>
                        {
                            {"function", "CreateAConversation" },
                            {"debugNote", "error closing task" },
                            {"vsoId", vsoId.ToString() },
                        });

                    throw;
                }
            }
        }

        public static async Task<EndUserAndAgentConversationMappingState> Get(IMessageActivity newActivity)
        {
            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, newActivity))
            {
                var botDataStore = scope.Resolve<IBotDataStore<BotData>>();
                var botConversationData = await botDataStore.LoadAsync(Address.FromActivity(newActivity),
                    BotStoreType.BotConversationData,
                    CancellationToken.None);
                return Get(botConversationData);
            }
        }

        public async Task SaveInVso(string vsoId)
        {
            Uri uri = new Uri(VsoHelper.Uri);

            JsonPatchDocument patchDocument = new JsonPatchDocument
                {
                    new JsonPatchOperation
                    {
                        Operation = Operation.Add, Path = $"/fields/{VsoHelper.EndUserConversationIdFieldName}", Value = EndUserConversationId
                    },
                    new JsonPatchOperation
                    {
                        Operation = Operation.Add, Path = $"/fields/{VsoHelper.AgentConversationIdFieldName}", Value = AgentConversationId
                    },
                    new JsonPatchOperation
                    {
                        Operation = Operation.Add, Path = $"/fields/{VsoHelper.EndUserIdFieldName}", Value = EndUserId
                    },
                    new JsonPatchOperation
                    {
                        Operation = Operation.Add, Path = $"/fields/{VsoHelper.EndUserNameFieldName}", Value = EndUserName
                    },
                };

            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = VsoHelper.GetWorkItemTrackingHttpClient())
            {
                try
                {
                    TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem result =
                        await workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, Convert.ToInt32(vsoId));

                    Trace.TraceInformation($"Project {vsoId} successfully updated. {this}");
                }
                catch (Exception ex)
                {
                    WebApiConfig.TelemetryClient.TrackException(ex, new Dictionary<string, string>
                        {
                            {"function", "SaveInVso" },
                            {"debugNote", "Failed to update project" },
                            {"vsoId", vsoId },
                        });
                    throw;
                }
            }
        }

        public static EndUserAndAgentConversationMappingState Get(BotData botData)
        {
            return new EndUserAndAgentConversationMappingState(
                botData.GetProperty<string>(VsoIdKey),
                botData.GetProperty<string>(EndUserNameKey),
                botData.GetProperty<string>(EndUserIdKey),
                botData.GetProperty<string>(EndUserConversationIdKey),
                botData.GetProperty<string>(AgentConversationIdKey)
            );
        }

        public async Task SaveIn(IMessageActivity messageActivity)
        {
            try
            {
                using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, messageActivity))
                {
                    var botDataStore = scope.Resolve<IBotDataStore<BotData>>();

                    var address = Address.FromActivity(messageActivity);
                    var botConversationData = await botDataStore.LoadAsync(address,
                        BotStoreType.BotConversationData,
                        CancellationToken.None);

                    SetProperties(botConversationData);

                    await botDataStore.SaveAsync(Address.FromActivity(messageActivity),
                        BotStoreType.BotConversationData,
                        botConversationData,
                        CancellationToken.None);

                    // confirm we read it back correctly. 
                    var savedState = await EndUserAndAgentConversationMappingState.Get(messageActivity);

                    if (savedState.EndUserName != EndUserName || savedState.EndUserConversationId != EndUserConversationId)
                        throw new Exception("Failed to read back state from activity after saving it");
                }
            }
            catch (Exception e) // <-- exception raised "Object reference not set to an instance of an object"
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                    {
                        {"function", "SaveIn" },
                        {"debugNote", "Failed to save project in conversation" },
                    });
                throw;
            }
        }

        public async Task SaveInEx(IMessageActivity messageActivity)
        {
            try
            {
                using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, messageActivity))
                {
                    var botDataStore = scope.Resolve<IBotDataStore<BotData>>();

                    var address = Address.FromActivity(messageActivity);
                    var botConversationData = await botDataStore.LoadAsync(address,
                        BotStoreType.BotConversationData,
                        CancellationToken.None);

                    SetProperties(botConversationData);

                    await botDataStore.SaveAsync(Address.FromActivity(messageActivity),
                        BotStoreType.BotConversationData,
                        botConversationData,
                        CancellationToken.None);

                    // confirm we read it back correctly. 
                    var savedState = await Get(messageActivity);

                    if (savedState.EndUserName != EndUserName || savedState.EndUserConversationId != EndUserConversationId)
                        throw new Exception("Failed to read back state from activity after saving it");
                }
            }
            catch (Exception e) // <-- exception raised "Object reference not set to an instance of an object"
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                    {
                        {"function", "SaveInEx" },
                        {"debugNote", "Failed to save project in conversation" },
                    });
                throw;
            }
        }



        public override string ToString()
        {
            return $"VsoId: {VsoId} \n\n" +
                   $"EndUserName: {EndUserName} \n\n" +
                   $"EndUserId: {EndUserId} \n\n" +
                   $"EndUserConversationId: {EndUserConversationId} \n\n" +
                   $"AgentConversationId: {AgentConversationId}";
        }
    }
}