using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Office.EIBot.Service.dialogs.Agent;
using Microsoft.Office.EIBot.Service.dialogs.EndUser;
using Microsoft.Office.EIBot.Service.dialogs.Exception;
using Microsoft.Teams.TemplateBotCSharp;
using static System.String;
using Activity = Microsoft.Bot.Connector.Activity;

namespace Microsoft.Office.EIBot.Service.utility
{
    public static class ActivityHelper
    {
        public const string SmsChannelId = "sms";
        public const string MsTeamChannelId = "msteams";
        private const string SmsServiceEndpoint = "https://sms.botframework.com";
        public const string TeamsServiceEndpoint = "https://smba.trafficmanager.net/amer/";

        public static IDialog<object> GetRootDialog(Activity activity)
        {
            IDialog<object> dialog = null;

            var properties = new Dictionary<string, string>
            {
                {"channelId", activity.ChannelId},
                {"fromName", activity.From.Name},
                {"recipientName", activity.Recipient.Name},
            };

            if (activity.ChannelId == SmsChannelId)
            {
                //properties.Add("dialog", "UserSmsRootDialog");
                //dialog = new ExceptionHandlerDialog<object>(new UserSmsRootDialog(),
                //displayException: true);
                dialog = new ExceptionHandlerDialog<object>(new UserRootDialog(),
                    displayException: true);
            }
            else
            {
                try
                {
                    if (activity.Conversation.ConversationType == "personal")
                    {
                        dialog = new ExceptionHandlerDialog<object>(new UserRootDialog(),
                            displayException: true);
                    }
                    else
                    {
                        properties.Add("dialog", "AgentRootDialog");
                        dialog = new ExceptionHandlerDialog<object>(new AgentRootDialog(),
                            displayException: true);
                    }
                }
                catch (Exception e)
                {
                    WebApiConfig.TelemetryClient.TrackException(e, properties);
                    dialog = new ExceptionHandlerDialog<object>(new UserRootDialog(),
                        displayException: true);
                }
            }

            WebApiConfig.TelemetryClient.TrackEvent("GetRootDialog", properties);

            return dialog;
        }

        public static bool IsConversationPersonal(IMessageActivity activity)
        {
            Trace.TraceInformation($"IsConversationPersonal called from channel: {activity.ChannelId}");
            if (activity.ChannelId == SmsChannelId) return true;
            try
            {
                foreach (var property in activity.Conversation.Properties)
                {
                    if (property.Key == "conversationType" && property.Value.ToString() == "personal") return true;
                }

                return false;
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"function", "IsConversationPersonal"},
                    {
                        "debugNote",
                        "Unable to determine if conversation is personal from activity in MessageController. " +
                        $"Treating this as personal chat"
                    },
                });

                return true;
            }
        }

        public static async Task<ResourceResponse> SendMessageToUserEx(IMessageActivity activity,
            string username,
            string userId,
            string messageToSend,
            string vsoId)
        {
            try
            {
                var userAccount = new ChannelAccount(userId, username);

                bool isSms = IsPhoneNumber(username);

                var serviceUrl = isSms ? SmsServiceEndpoint : activity.ServiceUrl;

                if (isSms)
                {
                    MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);
                }

                var botPhoneNumber = ConfigurationManager.AppSettings["BotPhoneNumber"];
                var botAccount = isSms
                    ? new ChannelAccount(botPhoneNumber, botPhoneNumber)
                    : activity.Recipient;

                using (ConnectorClient connector = await BotConnectorUtility.BuildConnectorClientAsync(serviceUrl))
                {
                    var conversation = connector.Conversations.CreateOrGetDirectConversation(botAccount,
                        userAccount, activity.GetChannelData<TeamsChannelData>().Tenant.Id);

                    IMessageActivity message = Activity.CreateMessageActivity();
                    message.From = botAccount;
                    message.Recipient = userAccount;
                    message.Conversation = new ConversationAccount(id: conversation.Id);
                    message.Text = $"[Human - {activity.From.Name}] {messageToSend}";
                    message.TextFormat = "plain";
                    message.Locale = "en-Us";
                    message.ChannelId = isSms ? SmsChannelId : MsTeamChannelId;
                    message.ServiceUrl = serviceUrl;

                    var endUserMessageActivity = (Activity) message;
                    var retryPolicy = BotConnectorUtility.BuildRetryPolicy();
                    ResourceResponse response = await retryPolicy.ExecuteAsync(async () =>
                        await connector.Conversations.SendToConversationAsync(endUserMessageActivity));

                    Trace.TraceInformation($"[SUCCESS]: SendMessageToUserEx. Message={messageToSend}. " +
                                           $"response id ={response.Id} vsoId={vsoId} ");

                    WebApiConfig.TelemetryClient.TrackEvent("SendMessageToUserEx", new Dictionary<string, string>
                    {
                        {"endUser", username},
                        {"messageToSend", messageToSend},
                        {"endUserConversationId", response.Id},
                        {"vsoId", vsoId},
                    });

                    return response;
                }
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"function", "SendMessageToUserEx"},
                    {"endUser", username},
                    {"messageToSend", messageToSend},
                    {"vsoId", vsoId},
                });
                throw;
            }
        }

        public static bool IsPhoneNumber(string username) => Regex.IsMatch(username, "[0-9]{7,}");

        public static async Task<ResourceResponse> SendMessageToAgentAsReplyToConversationInAgentsChannel(
            IMessageActivity activity,
            string messageToSend,
            string agentConversationId,
            int vsoId)
        {
            var propertiesForLogging = new Dictionary<string, string>
            {
                {"function", "SendMessageToAgentAsReplyToConversationInAgentsChannel"},
                {"endUser", activity.From.Name},
                {"messageToSend", messageToSend},
                {"agentConversationId", agentConversationId},
                {"vsoId", vsoId.ToString()},
            };

            try
            {
                var isSms = IsPhoneNumber(activity.From.Name);

                ChannelAccount botAccount = isSms ? await IdTable.GetBotId() : activity.Recipient;

                var serviceUrl = TeamsServiceEndpoint;

                using (ConnectorClient connector = await BotConnectorUtility.BuildConnectorClientAsync(serviceUrl))
                {
                    IMessageActivity message = Activity.CreateMessageActivity();
                    message.From = botAccount;
                    message.ReplyToId = agentConversationId;
                    message.Conversation = new ConversationAccount
                    {
                        Id = agentConversationId,
                        IsGroup = true,
                    };
                    IEnumerable<ChannelAccount> agentIds = await OnlineStatus.GetAgentIds();
                    var channelAccounts = agentIds as ChannelAccount[] ?? agentIds.ToArray();
                    string atMentions = Empty;
                    if (channelAccounts.Any())
                    {
                        //message.Entities = new List<Entity>(channelAccounts.Select(account => 
                        //    new Mention(account, "@" + account.Name.Replace(" ", "_"), "mention")));
                        atMentions = Join(" ", channelAccounts.Select(ca => "@" + ca.Name));
                    }

                    message.Text = $"{atMentions} [{activity.From.Name}]: {messageToSend}";
                    message.TextFormat = "plain";
                    message.ServiceUrl = TeamsServiceEndpoint;
                    message.ChannelData = new Dictionary<string, object>
                    {
                        ["teamsChannelId"] = "19:c20b196747424d8db51f6c00a8a9efa8@thread.skype",
                        ["notification"] = new Dictionary<string, object> {{"alert", true}}
                    };

                    ResourceResponse response = await BotConnectorUtility.BuildRetryPolicy().ExecuteAsync(async ()
                        => await connector.Conversations.SendToConversationAsync((Activity) message));
                    Trace.TraceInformation(
                        $"[SUCCESS]: SendMessageToAgentAsReplyToConversationInAgentsChannel. Message={messageToSend}. " +
                        $"response id ={response.Id} agentConversationId={agentConversationId} ");

                    propertiesForLogging.Add("replyMessageId", response.Id);
                    WebApiConfig.TelemetryClient.TrackEvent("SendMessageToAgentAsReplyToConversationInAgentsChannel",
                        propertiesForLogging);

                    return response;
                }
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, propertiesForLogging);
                throw;
            }
        }

        /// <summary>
        /// This is only here for reference purposes. Not used. 
        /// </summary>
        /// <param name="endUserActivity"></param>
        /// <param name="messageToSend"></param>
        /// <param name="agentConversationId"></param>
        /// <returns></returns>
        public static async Task<ConversationResourceResponse> CreateAConversation(IMessageActivity endUserActivity,
            string messageToSend,
            string agentConversationId)
        {
            var botAccount = endUserActivity.Recipient;

            try
            {
                // To create a new reply chain                      
                var channelData = new Dictionary<string, object>
                {
                    ["teamsChannelId"] = "19:c20b196747424d8db51f6c00a8a9efa8@thread.skype",
                    ["notification"] = new Dictionary<string, object>() {{"alert", true}}
                };

                IMessageActivity agentMessage = Activity.CreateMessageActivity();
                agentMessage.From = botAccount;
                agentMessage.Type = ActivityTypes.Message;
                agentMessage.Text = messageToSend;
                agentMessage.ChannelId = MsTeamChannelId;
                agentMessage.ServiceUrl = endUserActivity.ServiceUrl;
                agentMessage.ReplyToId = agentConversationId;

                var agentMessageActivity = (Activity) agentMessage;

                var conversationParams = new ConversationParameters()
                {
                    IsGroup = true,
                    Bot = botAccount,
                    Members = null,
                    Activity = agentMessageActivity,
                    ChannelData = channelData,
                };

                using (ConnectorClient connector =
                    await BotConnectorUtility.BuildConnectorClientAsync(endUserActivity.ServiceUrl))
                {
                    var createResponse = await BotConnectorUtility.BuildRetryPolicy().ExecuteAsync(async () =>
                        await connector.Conversations.CreateConversationAsync(conversationParams));
                    return createResponse;
                }
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"function", "CreateAConversation"},
                    {"messageToSend", messageToSend},
                    {"agentConversationId", agentConversationId},
                });

                throw;
            }
        }

        public static bool HasAttachment(IMessageActivity activity)
        {
            // todo: find way to determine if activity has attachment
            return false; //activity.Attachments.Any();
        }
    }
}