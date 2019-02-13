﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.ExpertConnect;
using Microsoft.ExpertConnect.Dialogs;
using Microsoft.ExpertConnect.Helpers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.ExpertConnect.Models;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;

namespace PPTExpertConnect
{
    public class ExpertConnect : IBot
    {
        private const string PreCompletionSelectionPath = "PreCompletionSelection";
        private const string UserToSelectProjectStatePath = "UserToSelectProjectState";
        private const string ReplyToUserPath = "ReplyToUser";
        private const string ReplyToAgentPath = "ReplyToAgent";

        private const string HelpText = @"Call Taranbir or Kapil ...";

        private const string WelcomeText = @"This bot will help you prepare PowerPoint files.
                                        Type anything to get logged in. Type 'logout' to sign-out";

        private readonly BotAccessors _accessors;
        private readonly AzureBlobTranscriptStore _transcriptStore;
        private readonly IdTable _idTable;
        private readonly EndUserAndAgentIdMapping _endUserAndAgentIdMapping;
        private readonly ILogger _logger;
        private DialogSet _dialogs;
        private CardBuilder cb;

        private readonly AppSettings _appSettings;
        private readonly SimpleCredentialProvider _botCredentials;

        private readonly string _OAuthConnectionSettingName;
        public ExpertConnect(
            AzureBlobTranscriptStore transcriptStore, 
            BotAccessors accessors, 
            ILoggerFactory loggerFactory, 
            IOptions<AppSettings> appSettings, 
            IdTable idTable, 
            EndUserAndAgentIdMapping endUserAndAgentIdMapping,
            ICredentialProvider credentials,
            IConfiguration configuration)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _OAuthConnectionSettingName = configuration.GetSection("OAuthConnectionSettingsName")?.Value;
            if (string.IsNullOrWhiteSpace(_OAuthConnectionSettingName))
            {
                throw new InvalidOperationException("OAuthConnectionSettingName must be configured prior to running the bot.");
            }

            _logger = loggerFactory.CreateLogger<ExpertConnect>();
            _logger.LogTrace("Turn start.");
            _accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));
            _transcriptStore = transcriptStore ?? throw new ArgumentNullException(nameof(transcriptStore)); // Test Mode ?
            _appSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));

            _idTable = idTable ?? throw new ArgumentNullException(nameof(idTable));
            _endUserAndAgentIdMapping = endUserAndAgentIdMapping;

            _botCredentials = (SimpleCredentialProvider)credentials ?? throw new ArgumentNullException(nameof(appSettings));
            
            cb = new CardBuilder(_appSettings);
            
            _dialogs = new DialogSet(accessors.DialogStateAccessor);
            
            _dialogs.Add(OAuthHelpers.Prompt(_OAuthConnectionSettingName));

            var authDialog = new WaterfallStep[] {PromptStepAsync, LoginStepAsync};
            _dialogs.Add(new WaterfallDialog(DialogId.Auth, authDialog));

            _dialogs.Add(new IntroductionDialog(DialogId.Start, cb));
            _dialogs.Add(new TemplateDetailDialog(DialogId.DetailPath, cb));
            _dialogs.Add(new ExampleTemplateDialog(DialogId.ExamplePath, cb));
            _dialogs.Add(new ProjectDetailDialog(DialogId.PostSelectionPath, cb, _OAuthConnectionSettingName));
            _dialogs.Add(new ProjectRevisionDialog(DialogId.ProjectRevisionPath, cb));
            _dialogs.Add(new ProjectCompleteDialog(DialogId.ProjectCompletePath, cb));

            var replyToUserSteps = new WaterfallStep[] { ReplyToUserStep };
            _dialogs.Add(new WaterfallDialog(ReplyToUserPath, replyToUserSteps));

            var replyToAgentSteps = new WaterfallStep[] { ReplyToAgentStep };
            _dialogs.Add(new WaterfallDialog(ReplyToAgentPath, replyToAgentSteps));

            var agentToUserForPostProjectCompletionBranching = new WaterfallStep[]{ ShowPostProjectCompletionChoices };
            _dialogs.Add(new WaterfallDialog(PreCompletionSelectionPath, agentToUserForPostProjectCompletionBranching));

            var handleProjectCompletionReplyFromAgent = new WaterfallStep[] { PostCompletionChoiceSelection };
            _dialogs.Add(new WaterfallDialog(UserToSelectProjectStatePath, handleProjectCompletionReplyFromAgent));

            _dialogs.Add(new TextPrompt(DialogId.SimpleTextPrompt));
        }

        
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }
            var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // This bot is not case sensitive.
                var text = turnContext.Activity.Text.ToLowerInvariant();

                if (text == "help")
                {
                    await turnContext.SendActivityAsync(HelpText, cancellationToken: cancellationToken);
                    return;
                }

                if (text == "logout")
                {
                    var botAdapter = (BotFrameworkAdapter)turnContext.Adapter;
                    await botAdapter.SignOutUserAsync(turnContext, _OAuthConnectionSettingName, cancellationToken: cancellationToken);

                    #region DialogCleanupState
                    await dialogContext.CancelAllDialogsAsync(cancellationToken);
                    await _transcriptStore.DeleteTranscriptAsync(turnContext.Activity.ChannelId,
                        turnContext.Activity.Conversation.Id);
                    await _accessors.UserInfoAccessor.DeleteAsync(turnContext, cancellationToken);
                    await _accessors.DialogStateAccessor.DeleteAsync(turnContext, cancellationToken);
                    #endregion  
                    
                    await turnContext.SendActivityAsync("You have been signed out.", cancellationToken: cancellationToken);
                    await SendWelcomeMessageAsync(turnContext, cancellationToken);
                    goto End;
                }
                var token = await ((BotFrameworkAdapter)turnContext.Adapter)
                    .GetUserTokenAsync(turnContext, _OAuthConnectionSettingName, null, cancellationToken)
                    .ConfigureAwait(false);

                if (token != null)
                {
                    var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                    if (results.Status is DialogTurnStatus.Complete)
                    {
                        var userInfo = results.Result as UserInfo;
                        await _accessors.UserInfoAccessor.SetAsync(turnContext, userInfo, cancellationToken);

                        switch (userInfo?.State)
                        {
                            case UserDialogState.ProjectSelectExampleOptions:
                                await dialogContext.BeginDialogAsync(DialogId.ExamplePath, userInfo, cancellationToken);
                                break;
                            case UserDialogState.ProjectCollectTemplateDetails:
                                await dialogContext.BeginDialogAsync(DialogId.DetailPath, userInfo, cancellationToken);
                                break;
                            case UserDialogState.ProjectCollectDetails:
                                await dialogContext.BeginDialogAsync(DialogId.PostSelectionPath, userInfo,
                                    cancellationToken);
                                break;
                            case UserDialogState.ProjectCreated:
                                var agentConversationId = await CreateAgentConversationMessage(turnContext,
                                    $"PowerPoint request from {turnContext.Activity.From.Name} via {turnContext.Activity.ChannelId}",
                                    cb.V2VsoTicketCard(251, "https://www.microsoft.com"));
                                
                                await _endUserAndAgentIdMapping.CreateNewMapping("vsoTicket-251", // Obtain this information from userInfo Class
                                    turnContext.Activity.From.Name,
                                    turnContext.Activity.From.Id,
                                    JsonConvert.SerializeObject(turnContext.Activity.GetConversationReference()),
                                    agentConversationId);
                                break;
                            case UserDialogState.ProjectUnderRevision:
                                var vsoTicketForUser =
                                    await _endUserAndAgentIdMapping.GetVsoTicketFromUserID(turnContext.Activity.From.Id);

                                var agentInfo = await _endUserAndAgentIdMapping.GetAgentConversationId(vsoTicketForUser);

                                await SendMessageToAgentAsReplyToConversationInAgentsChannel(turnContext, turnContext.Activity.Text,
                                    agentInfo, vsoTicketForUser);
                                userInfo.State = UserDialogState.ProjectInOneOnOneConversation;
                                await _accessors.UserInfoAccessor.SetAsync(turnContext, userInfo, cancellationToken);
                                break;
                            default:
                                break;
                        }
                    }

                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        var userProfile = await _accessors.UserInfoAccessor.GetAsync(turnContext, () => new UserInfo(), cancellationToken);

                        // Move the following code outside later ?
                        var didAgentUseACommand = DidAgentUseCommandOnBot(turnContext) || false;

                        if (didAgentUseACommand)
                        {
                            switch (GetCommandFromAgent(_appSettings.BotName, turnContext.Activity.Text))
                            {
                                case "reply to user":
                                    await dialogContext.BeginDialogAsync(ReplyToUserPath, null, cancellationToken);
                                    break;
                                case "project completed":
                                    await dialogContext.BeginDialogAsync(PreCompletionSelectionPath, null, cancellationToken);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else if (userProfile.State == UserDialogState.ProjectInOneOnOneConversation)
                        {
                            await dialogContext.BeginDialogAsync(ReplyToAgentPath, null, cancellationToken);
                        }
                        else if (userProfile.State == UserDialogState.ProjectCompleted)
                        {
                            await dialogContext.BeginDialogAsync(UserToSelectProjectStatePath, null, cancellationToken);
                        }
                        else
                        {
                            await dialogContext.BeginDialogAsync(DialogId.Start, userProfile, cancellationToken);
                        }
                    }
                }
                else
                {
                    await dialogContext.BeginDialogAsync(DialogId.Auth, null, cancellationToken);
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                bool isGroup = turnContext.Activity.Conversation.IsGroup ?? false;
                if (turnContext.Activity.ChannelId == "msteams" && isGroup) 
                {
                    await SaveAgentChannelIdInAzureStore(turnContext, _botCredentials);
                }
                await SaveBotIdInAzureStorage(turnContext, _appSettings.BotName);
            }
            else if (turnContext.Activity.Type == ActivityTypes.Invoke || turnContext.Activity.Type == ActivityTypes.Event)
            {
                // This handles the MS Teams Invoke Activity sent when magic code is not used.
                // See: https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/authentication/auth-oauth-card#getting-started-with-oauthcard-in-teams
                // The Teams manifest schema is found here: https://docs.microsoft.com/en-us/microsoftteams/platform/resources/schema/manifest-schema
                // It also handles the Event Activity sent from the emulator when the magic code is not used.
                // See: https://blog.botframework.com/2018/08/28/testing-authentication-to-your-bot-using-the-bot-framework-emulator/
                await dialogContext.ContinueDialogAsync(cancellationToken);
                if (!turnContext.Responded)
                {
                    await dialogContext.BeginDialogAsync(DialogId.Auth, cancellationToken: cancellationToken); // Begin auth or start ?
                }
                else
                {
                    await dialogContext.BeginDialogAsync(DialogId.Start, null, cancellationToken);
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected", cancellationToken: cancellationToken);
            }

            End:
            // Save the dialog state into the conversation state.
            await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);

            // Save the user profile updates into the user state.
            await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        #region Auth
        /// <summary>
        /// This <see cref="WaterfallStep"/> prompts the user to log in.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private static async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            return await step.BeginDialogAsync(OAuthHelpers.LoginPromptDialogId, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// In this step we check that a token was received and prompt the user as needed.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            var tokenResponse = (TokenResponse)step.Result;
            if (tokenResponse != null)
            {
                var userProfile = await _accessors.UserInfoAccessor.GetAsync(step.Context, () => new UserInfo(), cancellationToken);
                userProfile.Token = tokenResponse;

                var client = GraphClient.GetAuthenticatedClient(tokenResponse.Token);
                var user = await GraphClient.GetMeAsync(client);
                await step.Context.SendActivityAsync($"Kon'nichiwa { user.DisplayName}! You are now logged in.", cancellationToken: cancellationToken);
                return await step.EndDialogAsync(null, cancellationToken); // Maybe just end ??
            }

            await step.Context.SendActivityAsync("Login was not successful please try again. Aborting.", cancellationToken: cancellationToken);
            return await step.ReplaceDialogAsync(DialogId.Auth, null, cancellationToken);

        }

        /// <summary>
        /// Greet new users as they are added to the conversation.
        /// </summary>
        /// <param name="turnContext">Provides the <see cref="ITurnContext"/> for the turn of the bot.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the Turn operation.</returns>
        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(
                $"Welcome to ExpertConnect {turnContext.Activity.From.Name}. {WelcomeText}",
                cancellationToken: cancellationToken);
        }
        #endregion

        #region ReplyToUser/Agent

        private async Task<DialogTurnResult> ReplyToUserStep(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            var message = 
                    extractMessageFromCommand(_appSettings.BotName, "reply to user", context.Context.Activity.Text);

            var endUserInfo = await _endUserAndAgentIdMapping.GetEndUserInfo("vsoTicket-251");

          var userInfo =
                await _accessors.UserInfoAccessor.GetAsync(context.Context, () => new UserInfo(), cancellationToken);
            userInfo.State = UserDialogState.ProjectInOneOnOneConversation;

            await SendMessageToUserEx(context.Context,
                endUserInfo,
                message,
                "vsoTicket-251", 
                cancellationToken);

            await context.Context.SendActivityAsync("Message has been sent to user", null, null, cancellationToken);

            return await context.EndDialogAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ReplyToAgentStep(WaterfallStepContext context,
            CancellationToken cancellationToken)
        {
            var message = context.Context.Activity.Text;
            if (message.Equals(string.Empty))
            {
                return await context.EndDialogAsync(null, cancellationToken);
            }

            var vsoTicketForUser =
                await _endUserAndAgentIdMapping.GetVsoTicketFromUserID(context.Context.Activity.From.Id);

            var agentInfo = await _endUserAndAgentIdMapping.GetAgentConversationId(vsoTicketForUser);

            await SendMessageToAgentAsReplyToConversationInAgentsChannel(context.Context, message, agentInfo, vsoTicketForUser);

            return await context.EndDialogAsync(null, cancellationToken);
        }

        #endregion

        #region PostProjectCompletion
        
        private async Task<DialogTurnResult> ShowPostProjectCompletionChoices(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var userInfo = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);
            userInfo.State = UserDialogState.ProjectCompleted;

            var endUserInfo = await _endUserAndAgentIdMapping.GetEndUserInfo("vsoTicket-251");

           await SendCardToUserEx(stepContext.Context, endUserInfo, cb.V2PresentationResponse(endUserInfo.Name), "vsoTicket-251", cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> PostCompletionChoiceSelection(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var choice = stepContext.Context.Activity.Text;
            var userInfo =
                await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);

            if (choice.Equals(Constants.Complete))
            {
                return await stepContext.ReplaceDialogAsync(DialogId.ProjectCompletePath, userInfo, cancellationToken);
            }
            if (choice.Equals(Constants.Revision))
            {
                return await stepContext.ReplaceDialogAsync(DialogId.ProjectRevisionPath, userInfo, cancellationToken);
            }

            //TODO: handle case of message not a part of the two texts
            return null;
        }
        #endregion

        #region Helpers

        private async Task SaveBotIdInAzureStorage(ITurnContext context, string botName)
        {
            try
            {
                if (context.Activity.Recipient.Name.Equals(botName))
                {
                    await _idTable.SetBotId(context.Activity.Recipient);
                }
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
                // Trace.TraceError($"Error setting bot id. {e}");
            }
        }
        private async Task SaveAgentChannelIdInAzureStore(ITurnContext context, SimpleCredentialProvider credentials)
        {
            try
            {
                var connectorClient = await BotConnectorUtility.BuildConnectorClientAsync(
                    credentials.AppId, credentials.Password, context.Activity.ServiceUrl);

                var ci = GetChannelId(connectorClient, context, _appSettings.AgentChannelName);
                await _idTable.SetAgentChannel(ci.Name, ci.Id);
            }
            catch (SystemException e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }
        private static ChannelInfo GetChannelId(ConnectorClient connectorClient, ITurnContext context, string channelName)
        {
            var teamInfo = context.Activity.GetChannelData<TeamsChannelData>().Team;
            ConversationList channels = connectorClient.GetTeamsConnectorClient().Teams.FetchChannelList(teamInfo.Id);
            var channelInfo = channels.Conversations.FirstOrDefault(c => c.Name != null && c.Name.Equals(channelName));
            if (channelInfo == null) throw new System.Exception($"{channelName} doesn't exist in {context.Activity.GetChannelData<TeamsChannelData>().Team.Name} Team");
            return channelInfo;
        }
        private async Task<string> CreateAgentConversationMessage(ITurnContext context, string topicName, AdaptiveCard cardToSend)
        {
            var serviceUrl = context.Activity.ServiceUrl;
            var agentChannelInfo = await _idTable.GetAgentChannelInfo();
            ChannelAccount botMsTeamsChannelAccount = await _idTable.GetBotId();

            var connectorClient =
                BotConnectorUtility.BuildConnectorClientAsync(
                    _botCredentials.AppId,
                    _botCredentials.Password,
                    serviceUrl);

            try
            {
                var channelData = new TeamsChannelData { Channel = agentChannelInfo, Notification = new NotificationInfo(true)};

                IMessageActivity agentMessage = Activity.CreateMessageActivity();
                agentMessage.From = botMsTeamsChannelAccount;
                //                agentMessage.Recipient =
                //                    new ChannelAccount(ConfigurationManager.AppSettings["AgentToAssignVsoTasksTo"]);
                agentMessage.Type = ActivityTypes.Message;
                agentMessage.ChannelId = "msteams";
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
                        => await connectorClient.Result.Conversations.CreateConversationAsync(conversationParams));

                return conversationResourceResponse.Id;
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
                throw;
            }
        }
        private async Task SendMessageToUserEx(ITurnContext context,
            EndUserModel endUserModel,
            string messageToSend,
            string vsoId,
            CancellationToken cancellationToken)
        {
            try
            {
                BotAdapter adapter = context.Adapter;

                await adapter.ContinueConversationAsync(
                    _botCredentials.AppId,
                    JsonConvert.DeserializeObject<ConversationReference>(endUserModel.Conversation),
                    CreateCallback(messageToSend),
                    cancellationToken
                );
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
                throw;
            }
        }

        private async Task SendCardToUserEx(ITurnContext context, EndUserModel endUserModel, AdaptiveCard card,
            string vsoId, CancellationToken cancellationToken)
        {
            try
            {
                BotAdapter adapter = context.Adapter;
                await adapter.ContinueConversationAsync(
                    _botCredentials.AppId,
                    JsonConvert.DeserializeObject<ConversationReference>(endUserModel.Conversation),
                    CreateCallback(card),

                    cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        

        private BotCallbackHandler CreateCallback(string message)
        {
            return async (turnContext, token) =>
            {
                var dialogContext = await _dialogs.CreateContextAsync(turnContext, token);
                turnContext.Activity.Text = message;
                await turnContext.SendActivityAsync(message, null, null, token);
                await dialogContext.ContinueDialogAsync(cancellationToken: token);
            };
        }

        private BotCallbackHandler CreateCallback(AdaptiveCard card)
        {
            return async (context, token) =>
            {
                var dialogContext = await _dialogs.CreateContextAsync(context, token);
                await context.SendActivityAsync(DialogHelper.CreateAdaptiveCardAsActivity(card), token);
                await dialogContext.ContinueDialogAsync(token);
            };
        }

        private async Task<ResourceResponse> SendMessageToAgentAsReplyToConversationInAgentsChannel(
            ITurnContext context,
            string messageToSend,
            string agentConversationId,
            string vsoId)
        {
            try
            {
                ChannelAccount botAccount = await _idTable.GetBotId();

                var activity = context.Activity;
                var serviceUrl = "https://smba.trafficmanager.net/amer/";

                using (ConnectorClient connector = await BotConnectorUtility.BuildConnectorClientAsync(
                    _botCredentials.AppId,
                    _botCredentials.Password,
                    serviceUrl))
                {
                    IMessageActivity message = Activity.CreateMessageActivity();
                    message.From = botAccount;
                    message.ReplyToId = agentConversationId;
                    message.Conversation = new ConversationAccount
                    {
                        Id = agentConversationId,
                        IsGroup = true,
                    };

                    var agentChannelInfo = await _idTable.GetAgentChannelInfo();
                    var channelData = new TeamsChannelData() { Channel = agentChannelInfo, Notification = new NotificationInfo(true) };

                    message.Text = $"[{activity.From.Name}]: {messageToSend}";
                    message.TextFormat = "plain";
                    message.ServiceUrl = serviceUrl;
                    message.ChannelData = channelData;

                    ResourceResponse response = await BotConnectorUtility.BuildRetryPolicy().ExecuteAsync(async ()
                        => await connector.Conversations.SendToConversationAsync((Activity)message));
                   
                    return response;
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
                throw;
            }
        }

        private bool DidAgentUseCommandOnBot(ITurnContext context)
        {
            var isGroup = context.Activity.Conversation.IsGroup ?? false;
            var mentions = context.Activity.GetMentions();

            // TODO: mentions.length could crash if null!!!!
            return (isGroup && mentions.Length > 0 && mentions.FirstOrDefault().Text.Contains(_appSettings.BotName));
        }

        private string GetCommandFromAgent(string botName, string message)
        {
            var atBotPattern = new Regex($"^<at>({botName})</at>");
            var fullPattern = new Regex($"^<at>({botName})</at> (.*)// (.*)");
            if (atBotPattern.IsMatch(message))
            {
                return fullPattern.Match(message).Groups[2].Value;
            }
            return null;
        }

        private string extractMessageFromCommand(string botName, string command, string message)
        {
            var atBotPattern = new Regex($"^<at>({botName})</at>");
            var commandPattern = new Regex($" ({command}) ");
            var fullPattern = new Regex($"^<at>({botName})</at> ({command}) (.*)");

            if (atBotPattern.IsMatch(message) && commandPattern.IsMatch(message))
            {
                return fullPattern.Match(message).Groups[3].Value;
            }

            return null;
        }
        #endregion
    }
    
}
