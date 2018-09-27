using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;
using Microsoft.VisualStudio.Services.Common;
using Activity = Microsoft.Bot.Connector.Activity;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    public static class ConversationHelpers
    {
        public static async Task<bool> RelayMessageToAgentIfThereIsAnOpenResearchProject(IDialogContext context)
        {
            int? vsoId = await GetResearchVsoIdFromContextOrVso(context);
            if (vsoId == null) return false;

            EndUserAndAgentConversationMappingState state =
                await EndUserAndAgentConversationMappingState.GetFromVso((int)vsoId);

            // Check when was the last time we sent message to agent
            var timeStampWhenLastMessageWasSentByAgent =
                await OnlineStatus.GetTimeWhenMemberWasLastActive(OnlineStatus.AgentMemberType);
            var timeSpan = DateTime.UtcNow.Subtract((DateTime)timeStampWhenLastMessageWasSentByAgent);
            var timeDiffInSeconds = timeSpan.TotalSeconds;
            if (timeDiffInSeconds >= 30)
            {
                await context.PostWithRetryAsync($"My experts are working on Project #{vsoId}. " +
                                        $"Current status of this project is {await VsoHelper.GetProjectStatus((int)vsoId)}. " +
                                        "Either experts are busy or offline at the moment. " +
                                        $"They were online {timeSpan.TimeAgo()}. Please wait. ");
            }

            IMessageActivity messageActivity = (IMessageActivity)context.Activity;
            await ActivityHelper.SendMessageToAgentAsReplyToConversationInAgentsChannel(
                messageActivity,
                messageActivity.Text,
                state.AgentConversationId,
                (int)vsoId);

            await OnlineStatus.SetMemberActive(context.Activity.From.Name,
                context.Activity.From.Id,
                OnlineStatus.EndUserMemberType);

            return true;

        }

        private static async Task<int?> GetResearchVsoIdFromContextOrVso(IDialogContext context)
        {
            var properties = new Dictionary<string, string>
            {
                {"class", "ConversationHelpers" },
                {"function", "GetResearchVsoIdFromContextOrVso" },
                {"from", context.Activity.From.Name }
            };

            int? vsoId = await GetVsoIdFromConversation(context);
            // if vsoId is null, try getting it from VSO.
            if (vsoId == null)
            {
                try
                {
                    var workItems = await VsoHelper.GetWorkItemsForUser(
                        ActivityHelper.IsPhoneNumber(context.Activity.From.Name) ? VsoHelper.VirtualAssistanceTaskType : VsoHelper.ResearchTaskType,
                        context.Activity.From.Name);
                    if (workItems != null)
                    {
                        vsoId = workItems.Select(wi => wi.Id).FirstOrDefault();
                    }
                }
                catch (System.Exception e)
                {

                    WebApiConfig.TelemetryClient.TrackException(e, properties);
                }
            }

            properties.Add("vsoId", vsoId != null ? vsoId.ToString() : "not set");

            WebApiConfig.TelemetryClient.TrackEvent("GetResearchVsoIdFromContextOrVso", properties);

            return vsoId;
        }

        public static async Task<int?> GetVsoIdFromConversation(IDialogContext endUserDialogContext)
        {
            int? vsoId = null;
            try
            {
                string status = "not set";
                if (endUserDialogContext.ConversationData.TryGetValue("VsoId", out string vsoIdFromConversation))
                {
                    int convertedVsoId = Convert.ToInt32(vsoIdFromConversation);
                    status = await VsoHelper.GetProjectStatus(convertedVsoId);
                    if (!status.ToLower().Contains("closed"))
                    {
                        vsoId = convertedVsoId;
                    }
                }
                WebApiConfig.TelemetryClient.TrackEvent("GetVsoIdFromConversation", new Dictionary<string, string>
                {
                    {"class", "HelloDialog" },
                    {"function", "GetVsoIdFromConversation" },
                    {"from", endUserDialogContext.Activity.From.Name },
                    {"vsoId", vsoId != null ? vsoId.ToString() : "not set" },
                    {"vsoIdStatus", status },
                });
            }
            catch (VssServiceException e)
            {
                if (e.Message.Contains("does not exist"))
                {
                    // we might have deleted this item. 

                    WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                    {
                        {"class", "HelloDialog" },
                        {"function", "GetVsoIdFromConversation" },
                        {"dialog", "HelloDialog" },
                        {"from", endUserDialogContext.Activity.From.Name }
                    });

                    return null;
                }
            }
            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"class", "HelloDialog" },
                    {"function", "GetVsoIdFromConversation" },
                    {"dialog", "HelloDialog" },
                    {"from", endUserDialogContext.Activity.From.Name }
                });
                throw;
            }
            return vsoId;
        }

        public static async Task<ConversationResourceResponse> CreateAgentConversation(ChannelInfo targetChannelInfo,
            AdaptiveCard card,
            string topicName,
            ConnectorClient connector,
            int vsoTicketNumber,
            IMessageActivity endUserActivity)
        {
            try
            {
                var channelData = new TeamsChannelData { Channel = targetChannelInfo };

                IMessageActivity agentMessage = Activity.CreateMessageActivity();
                agentMessage.From = endUserActivity.Recipient;
                agentMessage.Recipient = new ChannelAccount("mamottol@microsoft.com");
                agentMessage.Type = ActivityTypes.Message;
                agentMessage.ChannelId = "msteam";
                agentMessage.ServiceUrl = endUserActivity.ServiceUrl;

                agentMessage.Attachments = new List<Attachment>
                {
                    new Attachment {ContentType = AdaptiveCard.ContentType, Content = card}
                };

                var agentMessageActivity = (Activity)agentMessage;

                ConversationParameters conversationParams = new ConversationParameters(
                    isGroup: true,
                    bot: null,
                    members: null,
                    topicName: topicName,
                    activity: agentMessageActivity,
                    channelData: channelData);

                var conversationResourceResponse = await BotConnectorUtility.BuildRetryPolicy().ExecuteAsync(async () 
                    => await connector.Conversations.CreateConversationAsync(conversationParams));

                Trace.TraceInformation($"[SUCCESS]: CreateAgentConversation. " +
                                       $"response id ={conversationResourceResponse.Id} vsoId={vsoTicketNumber} ");

                WebApiConfig.TelemetryClient.TrackEvent("CreateAgentConversation", new Dictionary<string, string>
                {
                    { "endUser", agentMessage.From.Name},
                    { "agentConversationId", conversationResourceResponse.Id},
                    { "vsoId", vsoTicketNumber.ToString()},
                });

                return conversationResourceResponse;
            }
            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"function", "CreateAgentConversation" },
                    {"endUser", endUserActivity.Recipient.Name},
                    {"vsoId", vsoTicketNumber.ToString() }
                });

                throw;
            }
        }
    }
}