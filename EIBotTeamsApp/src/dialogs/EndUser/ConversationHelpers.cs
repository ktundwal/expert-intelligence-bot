using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
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
        private const int MinutesToWaitBeforeSendingAutoReply = 10;
        private const string AutoReplySentOnKey = "AutoReplySentOn";
        private const int MinutesToWaitForAgentOnlineBeforeSendingAutoReply = 30;

        public static async Task<bool> RelayMessageToAgentIfThereIsAnOpenResearchProject(IDialogContext context)
        {
            int? vsoId = await GetResearchVsoIdFromVso(context.Activity.ChannelId, context.Activity.From.Name);
            if (vsoId == null) return false;

            var userProfile = await UserProfileHelper.GetUserProfile(context);
            context.UserData.SetValue(UserProfileHelper.UserProfileKey, userProfile);

            string agentConversationId = await VsoHelper.GetAgentConversationIdForVso((int)vsoId);
            if (string.IsNullOrEmpty(agentConversationId)) return false;
            
            await SendAutoReplyIfNeeded(context, vsoId);

            IMessageActivity messageActivity = (IMessageActivity)context.Activity;
            await ActivityHelper.SendMessageToAgentAsReplyToConversationInAgentsChannel(
                messageActivity,
                messageActivity.Text,
                agentConversationId,
                (int)vsoId);

            await OnlineStatus.SetMemberActive(context.Activity.From.Name,
                context.Activity.From.Id,
                OnlineStatus.EndUserMemberType);

            return true;
        }

        private static async Task SendAutoReplyIfNeeded(IDialogContext context, int? vsoId)
        {
            // Check when was the last time we sent message to agent
            var timeStampWhenLastMessageWasSentByAgent =
                            await OnlineStatus.GetTimeWhenMemberWasLastActive(OnlineStatus.AgentMemberType);
            var timeSinceLastMessageWasSentByAgent = DateTime.UtcNow.Subtract((DateTime)timeStampWhenLastMessageWasSentByAgent);
            bool autoReplyWasSentAWhileBack = DateTime.UtcNow.Subtract(GetAutoReplySentOnTimeStamp(context))
                                                  .TotalMinutes > MinutesToWaitBeforeSendingAutoReply;
            if (timeSinceLastMessageWasSentByAgent.TotalMinutes >= MinutesToWaitForAgentOnlineBeforeSendingAutoReply && autoReplyWasSentAWhileBack)
            {
                await context.PostWithRetryAsync($"Hi {UserProfileHelper.GetFriendlyName(context)}, " +
                                                 $"My experts are working on Project #{vsoId}. " +
                                        $"Current status of this project is {await VsoHelper.GetProjectStatus((int)vsoId)}. " +
                                        "Either experts are busy or offline at the moment. " +
                                        $"They were online {timeSinceLastMessageWasSentByAgent.TimeAgo()}. Please wait. ");
                SetAutoReplySentOnTimeStamp(context);
            }
        }

        private static DateTime GetAutoReplySentOnTimeStamp(IBotData context) => context.ConversationData.TryGetValue(
            AutoReplySentOnKey,
            out DateTime autoReplySentOn)
            ? autoReplySentOn
            : DateTime.MinValue;

        private static void SetAutoReplySentOnTimeStamp(IBotData context) => context.ConversationData.SetValue(AutoReplySentOnKey, DateTime.UtcNow);

        private static async Task<int?> GetResearchVsoIdFromVso(string channelId, string uniqueName)
        {
            var properties = new Dictionary<string, string>
            {
                {"class", "ConversationHelpers" },
                {"function", "GetResearchVsoIdFromVso" },
                {"channelId",  channelId},
                {"from",  uniqueName}
            };

            int? vsoId = null;
            try
            {
                var workItems = await VsoHelper.GetWorkItemsForUser(
                    VsoHelper.ResearchTaskType,
                    channelId,
                    uniqueName);
                if (workItems != null)
                {
                    vsoId = workItems.Select(wi => wi.Id).FirstOrDefault();
                }
            }
            catch (System.Exception e)
            {

                WebApiConfig.TelemetryClient.TrackException(e, properties);
            }
            properties.Add("vsoId", vsoId != null ? vsoId.ToString() : "not set");

            WebApiConfig.TelemetryClient.TrackEvent("GetResearchVsoIdFromVso", properties);

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

        public static async Task<string> CreateAgentConversationEx(IDialogContext context,
            string topicName,
            AdaptiveCard cardToSend,
            UserProfile endUserProfile)
        {
            string serviceUrl = GetServiceUrl(context);

            var agentChannelInfo = await IdTable.GetAgentChannelInfo();

            ChannelAccount botMsTeamsChannelAccount = context.Activity.ChannelId == ActivityHelper.SmsChannelId
                ? await IdTable.GetBotId()
                : context.Activity.From;

            using (var connectorClient = await BotConnectorUtility.BuildConnectorClientAsync(serviceUrl))
            {
                try
                {
                    var channelData = new TeamsChannelData { Channel = agentChannelInfo };

                    IMessageActivity agentMessage = Activity.CreateMessageActivity();
                    agentMessage.From = botMsTeamsChannelAccount;
                    agentMessage.Recipient =
                        new ChannelAccount(ConfigurationManager.AppSettings["AgentToAssignVsoTasksTo"]);
                    agentMessage.Type = ActivityTypes.Message;
                    agentMessage.ChannelId = ActivityHelper.MsTeamChannelId;
                    agentMessage.ServiceUrl = serviceUrl;

                    agentMessage.Attachments = new List<Attachment>
                    {
                        new Attachment {ContentType = AdaptiveCard.ContentType, Content = cardToSend}
                    };

                    var agentMessageActivity = (Activity)agentMessage;

                    ConversationParameters conversationParams = new ConversationParameters(
                        isGroup: true,
                        bot: null,
                        members: null,
                        topicName: topicName,
                        activity: agentMessageActivity,
                        channelData: channelData);

                    var conversationResourceResponse = await BotConnectorUtility.BuildRetryPolicy().ExecuteAsync(
                        async ()
                            => await connectorClient.Conversations.CreateConversationAsync(conversationParams));

                    Trace.TraceInformation(
                        $"[SUCCESS]: CreateAgentConversation. response id ={conversationResourceResponse.Id}");

                    WebApiConfig.TelemetryClient.TrackEvent("CreateAgentConversation", new Dictionary<string, string>
                    {
                        {"endUser", agentMessage.From.Name},
                        {"agentConversationId", conversationResourceResponse.Id},
                    });

                    return conversationResourceResponse.Id;
                }
                catch (System.Exception e)
                {
                    WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                    {
                        {"function", "CreateAgentConversation" }
                    });

                    throw;
                }
            }
        }

        private static string GetServiceUrl(IDialogContext context)
        {
            string serviceUrl;
            if (context.Activity.ChannelId == ActivityHelper.SmsChannelId)
            {
                // switch service url and trust it
                serviceUrl = ActivityHelper.TeamsServiceEndpoint;
                MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);
            }
            else
            {
                serviceUrl = context.Activity.ServiceUrl;
            }

            return serviceUrl;
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
                agentMessage.Recipient = new ChannelAccount(ConfigurationManager.AppSettings["AgentToAssignVsoTasksTo"]);
                agentMessage.Type = ActivityTypes.Message;
                agentMessage.ChannelId = ActivityHelper.MsTeamChannelId;
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