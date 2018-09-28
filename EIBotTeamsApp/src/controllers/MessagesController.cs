using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Office.EIBot.Service.Properties;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;
using Activity = Microsoft.Bot.Connector.Activity;
using Middleware = Microsoft.Office.EIBot.Service.middleware.Middleware;

namespace Microsoft.Office.EIBot.Service.controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Bot.Connector.Activity activity, CancellationToken cancellationToken)
        {
            var connectorClient = await BotConnectorUtility.BuildConnectorClientAsync(activity.ServiceUrl);

            try
            {
                if (activity.Type == ActivityTypes.Message)
                {
                    // Special handling for a command to simulate a reset of the bot chat
                    if (!(activity.Conversation.IsGroup ?? false) && (activity.Text == "/resetbotchat"))
                    {
                        return await HandleResetBotChatAsync(activity, cancellationToken);
                    }

                    //Set the Locale for Bot
                    activity.Locale = TemplateUtility.GetLocale(activity);

                    //Strip At mention from incoming request text
                    activity = Middleware.StripAtMentionText(activity);

                    //Convert incoming activity text to lower case, to match the intent irrespective of incoming text case
                    activity = Middleware.ConvertActivityTextToLower(activity);

                    // todo: enable tenant check
                    //var unexpectedTenantResponse = await RejectMessageFromUnexpectedTenant(activity, connectorClient);
                    //if (unexpectedTenantResponse != null) return unexpectedTenantResponse;

                    //await Conversation.SendAsync(activity, () => ActivityHelper.IsConversationPersonal(activity)
                    //    ? (IDialog<object>)new UserRootDialog()
                    //    : new AgentRootDialog());

                    await Conversation.SendAsync(activity, () => ActivityHelper.GetRootDialog(activity));

                    //await Conversation.SendAsync(activity, () => ActivityHelper.IsConversationPersonal(activity)
                    //    ? new ExceptionHandlerDialog<object>(new UserRootDialog(),
                    //        displayException: true)
                    //    : new ExceptionHandlerDialog<object>(new AgentRootDialog(),
                    //        displayException: true));
                }
                else if (activity.Type == ActivityTypes.MessageReaction)
                {
                    var reactionsAdded = activity.ReactionsAdded;
                    var reactionsRemoved = activity.ReactionsRemoved;
                    var replytoId = activity.ReplyToId;
                    Bot.Connector.Activity reply;

                    if (reactionsAdded != null && reactionsAdded.Count > 0)
                    {
                        reply = activity.CreateReply(Strings.LikeMessage);
                        await BotConnectorUtility.BuildRetryPolicy().ExecuteAsync(async () =>
                            await connectorClient.Conversations.ReplyToActivityAsync(reply));
                    }
                    else if (reactionsRemoved != null && reactionsRemoved.Count > 0)
                    {
                        reply = activity.CreateReply(Strings.RemoveLike);
                        await BotConnectorUtility.BuildRetryPolicy().ExecuteAsync(async () =>
                            await connectorClient.Conversations.ReplyToActivityAsync(reply));
                    }

                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                else if (activity.Type == ActivityTypes.Invoke) // Received an invoke
                {
                    // Handle ComposeExtension query
                    if (activity.IsComposeExtensionQuery())
                    {
                        WikipediaComposeExtension wikipediaComposeExtension = new WikipediaComposeExtension();
                        HttpResponseMessage httpResponse = null;

                        using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                        {
                            var botDataStore = scope.Resolve<IBotDataStore<BotData>>();
                            // Handle compose extension selected item
                            if (activity.Name == "composeExtension/selectItem")
                            {
                                // This handler is used to process the event when a user in Teams selects wiki item from wiki result
                                ComposeExtensionResponse selectedItemResponse = await wikipediaComposeExtension.HandleComposeExtensionSelectedItem(activity, botDataStore);
                                httpResponse = Request.CreateResponse<ComposeExtensionResponse>(HttpStatusCode.OK, selectedItemResponse);
                            }
                            else
                            {
                                // Handle the wiki compose extension request and returned the wiki result response
                                ComposeExtensionResponse composeExtensionResponse = await wikipediaComposeExtension.GetComposeExtensionResponse(activity, botDataStore);
                                httpResponse = Request.CreateResponse<ComposeExtensionResponse>(HttpStatusCode.OK, composeExtensionResponse);
                            }

                            var address = Address.FromActivity(activity);
                            await botDataStore.FlushAsync(address, CancellationToken.None);
                        }
                        return httpResponse;
                    }
                    //Actionable Message
                    else if (activity.IsO365ConnectorCardActionQuery())
                    {
                        // this will handle the request coming any action on Actionable messages
                        return await HandleO365ConnectorCardActionQuery(activity);
                    }
                    //PopUp SignIn
                    else if (activity.Name == "signin/verifyState")
                    {
                        // this will handle the request coming from PopUp SignIn 
                        return await PopUpSignInHandler(activity);
                    }
                    // Handle rest of the invoke request
                    else
                    {
                        var messageActivity = (IMessageActivity)null;

                        //this will parse the invoke value and change the message activity as well
                        messageActivity = InvokeHandler.HandleInvokeRequest(activity);

                        await Conversation.SendAsync(activity, () => ActivityHelper.GetRootDialog(activity));

                        //await Conversation.SendAsync(activity, () => ActivityHelper.IsConversationPersonal(activity)
                        //    ? (IDialog<object>) new UserRootDialog()
                        //    : new AgentRootDialog());

                        //await Conversation.SendAsync(messageActivity, () => ActivityHelper.IsConversationPersonal(messageActivity)
                        //    ? new ExceptionHandlerDialog<object>(new UserRootDialog(),
                        //        displayException: true)
                        //    : new ExceptionHandlerDialog<object>(new AgentRootDialog(),
                        //        displayException: true));

                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                }
                else
                {
                    await HandleSystemMessageAsync(activity, connectorClient, cancellationToken);
                }
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"class", "MessagesController" }
                });
                throw;
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);

            return response;
        }

        private async Task<HttpResponseMessage> RejectMessageFromUnexpectedTenant(Activity activity, ConnectorClient connectorClient)
        {
            //Set the OFFICE_365_TENANT_FILTER key in web.config file with Tenant Information
            //Validate bot for specific teams tenant if any
            string currentTenant = "#ANY#";
            try
            {
                currentTenant = activity.GetTenantId();
            }
            catch (Exception e)
            {
                Trace.TraceError($"Exception from activity.GetTenantId(): {e}");
            }

            if (Middleware.RejectMessageBasedOnTenant(activity, currentTenant))
            {
                Bot.Connector.Activity replyActivity = activity.CreateReply();
                replyActivity.Text = Strings.TenantLevelDeniedAccess;

                await BotConnectorUtility.BuildRetryPolicy().ExecuteAsync(async () =>
                    await connectorClient.Conversations.ReplyToActivityAsync(replyActivity));
                {
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
            }

            return null;
        }

        private async Task HandleSystemMessageAsync(Bot.Connector.Activity message, ConnectorClient connectorClient, CancellationToken cancellationToken)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // This shows how to send a welcome message in response to a conversationUpdate event

                // We're only interested in member added events
                if (message.MembersAdded?.Count > 0)
                {
                    // Determine if the bot was added to the team/conversation
                    var botId = message.Recipient.Id;
                    var botWasAdded = message.MembersAdded.Any(member => member.Id == botId);

                    // Create the welcome message to send
                    Bot.Connector.Activity welcomeMessage = message.CreateReply();
                    welcomeMessage.Attachments = new List<Attachment>
                    {
                        new HeroCard(Strings.HelpTitle)
                        {
                            Title = "Hello! I am Expert Intelligence Bot.",
                            Subtitle = "I am supported by experts who can work for you. " +
                                       "You can also request virtual assistance such as booking an appointment with car dealer. " +
                                       $"Send me a SMS at {ConfigurationManager.AppSettings["BotPhoneNumber"]}",
                        }.ToAttachment()
                    };

                    if (!(message.Conversation.IsGroup ?? false))
                    {
                        // 1:1 conversation event

                        // If the user hasn't received a first-run message yet, then send a message to the user
                        // introducing your bot and what it can do. Do NOT send this blindly, as your bot can receive
                        // spurious conversationUpdate events, especially if you use proactive messaging.
                        using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
                        {
                            var address = Address.FromActivity(message);
                            var botDataStore = scope.Resolve<IBotDataStore<BotData>>();
                            var botData = await botDataStore.LoadAsync(address, BotStoreType.BotUserData, cancellationToken);

                            if (!botData.GetProperty<bool>("IsFreSent"))
                            {
                                await connectorClient.Conversations.ReplyToActivityWithRetriesAsync(welcomeMessage, cancellationToken);

                                // Remember that we sent the welcome message already
                                botData.SetProperty("IsFreSent", true);
                                await botDataStore.SaveAsync(address, BotStoreType.BotUserData, botData, cancellationToken);
                            }
                            else
                            {
                                // First-run message has already been sent, so skip sending it again.
                                // Do not remove the check for IsFreSent above. Your bot can receive spurious conversationUpdate
                                // activities from chat service, so if you always respond to all of them, you will send random 
                                // welcome messages to users who have already received the welcome.
                            }
                        }
                    }
                    else
                    {
                        // Not 1:1 chat event (bot or user was added to a team or group chat)
                        if (botWasAdded)
                        {
                            // Bot was added to the team
                            // Send a message to the team's channel, introducing your bot and what you can do
                            await connectorClient.Conversations.ReplyToActivityWithRetriesAsync(welcomeMessage, cancellationToken);
                        }
                        else
                        {
                            // Other users were added to the team/conversation
                        }
                    }
                }
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
        }

        /// <summary>
        /// Handles a request from the user to simulate a new chat.
        /// </summary>
        /// <param name="message">The incoming message requesting the reset</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> HandleResetBotChatAsync(Bot.Connector.Activity message, CancellationToken cancellationToken)
        {
            // Forget everything we know about the user
            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
            {
                var address = Address.FromActivity(message);
                var botDataStore = scope.Resolve<IBotDataStore<BotData>>();
                await botDataStore.SaveAsync(address, BotStoreType.BotUserData, new BotData("*"), cancellationToken);
                await botDataStore.SaveAsync(address, BotStoreType.BotConversationData, new BotData("*"), cancellationToken);
                await botDataStore.SaveAsync(address, BotStoreType.BotPrivateConversationData, new BotData("*"), cancellationToken);
            }

            // If you need to reset the user state in other services your app uses, do it here.

            // Synthesize a conversation update event and simulate the bot receiving it
            // Note that this is a fake event, as Teams does not support deleting a 1:1 conversation and re-creating it
            var conversationUpdateMessage = new Bot.Connector.Activity
            {
                Type = ActivityTypes.ConversationUpdate,
                Id = message.Id,
                ServiceUrl = message.ServiceUrl,
                From = message.From,
                Recipient = message.Recipient,
                Conversation = message.Conversation,
                ChannelData = message.ChannelData,
                ChannelId = message.ChannelId,
                Timestamp = message.Timestamp,
                MembersAdded = new List<ChannelAccount> { message.From, message.Recipient },
            };
            return await this.Post(conversationUpdateMessage, cancellationToken);
        }

        /// <summary>
        /// Handles O365 connector card action queries.
        /// </summary>
        /// <param name="activity">Incoming request from Bot Framework.</param>
        /// <param name="connectorClient">Connector client instance for posting to Bot Framework.</param>
        /// <returns>Task tracking operation.</returns>

        private static async Task<HttpResponseMessage> HandleO365ConnectorCardActionQuery(Bot.Connector.Activity activity)
        {
            var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));

            // Get O365 connector card query data.
            O365ConnectorCardActionQuery o365CardQuery = activity.GetO365ConnectorCardActionQueryData();

            Bot.Connector.Activity replyActivity = activity.CreateReply();

            replyActivity.TextFormat = "xml";

            replyActivity.Text = $@"

            <h2>Thanks, {activity.From.Name}</h2><br/>

            <h3>Your input action ID:</h3><br/>

            <pre>{o365CardQuery.ActionId}</pre><br/>

            <h3>Your input body:</h3><br/>

            <pre>{o365CardQuery.Body}</pre>

        ";

            await connectorClient.Conversations.ReplyToActivityWithRetriesAsync(replyActivity);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        /// <summary>
        /// Handle the PopUp SignIn requests
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        private static async Task<HttpResponseMessage> PopUpSignInHandler(Bot.Connector.Activity activity)
        {
            var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));

            Bot.Connector.Activity replyActivity = activity.CreateReply();

            replyActivity.Text = $@"Authentication Successful";

            await connectorClient.Conversations.ReplyToActivityWithRetriesAsync(replyActivity);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}