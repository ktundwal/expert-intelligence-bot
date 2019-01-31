// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using PPTExpertConnect.Helpers;
using PPTExpertConnect.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Extensions.Configuration;
using DelegateAuthenticationProvider = Microsoft.Graph.DelegateAuthenticationProvider;
using GraphServiceClient = Microsoft.Graph.GraphServiceClient;

namespace PPTExpertConnect
{
    public class ExpertConnect : IBot
    {
        private readonly string Auth = "BeginAuth";
        private readonly string Start = "Start";
        private readonly string DetailPath = "Details";
        private readonly string ExamplePath = "Example";
        private readonly string PostSelectionPath = "PostSelection";

        private const string HelpText = @"Call Taranbir or Kapil ...";

        private const string WelcomeText = @"This bot will help you prepare PPT files.
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

            var start = new WaterfallStep[] { IntroductionStep, PostIntroductionStep };

            var authDialog = new WaterfallStep[] {PromptStepAsync, LoginStepAsync};

            var detailSteps = new WaterfallStep[]
            {
                PurposeStep,
                ColorVariationStep,
                IllustrationStep,
                PostDetailStep
            };

            var exampleSteps = new WaterfallStep[]
            {
                ShowExampleStep,
                ProcessExampleStep
            };

            var postSelectionSteps = new WaterfallStep[]
            {
                ImageOptions,
                ExtraInfoStep,
                UserInfoAddedStep,
                SummaryStep,
                TicketStep,
                End
            };

            var replyToUserSteps = new WaterfallStep[]
            {
                ReplyToUserStep
            };
            var replyToAgentSteps = new WaterfallStep[]
            {
                ReplyToAgentStep
            };

            _dialogs.Add(OAuthHelpers.Prompt(_OAuthConnectionSettingName));

            _dialogs.Add(new WaterfallDialog(Auth, authDialog));
            _dialogs.Add(new WaterfallDialog(Start, start));
            _dialogs.Add(new WaterfallDialog(DetailPath, detailSteps));
            _dialogs.Add(new WaterfallDialog(ExamplePath, exampleSteps));
            _dialogs.Add(new WaterfallDialog(PostSelectionPath, postSelectionSteps));

            _dialogs.Add(new WaterfallDialog("replyToUser", replyToUserSteps));
            _dialogs.Add(new WaterfallDialog("replyToAgent", replyToAgentSteps));

            // TODO: clean up to just one
            _dialogs.Add(new TextPrompt(UserData.FirstStep));
            _dialogs.Add(new TextPrompt(UserData.Purpose));
            _dialogs.Add(new TextPrompt(UserData.Style));
            _dialogs.Add(new TextPrompt(UserData.Color));
            _dialogs.Add(new TextPrompt(UserData.Visuals));
            _dialogs.Add(new TextPrompt(UserData.Images));
            _dialogs.Add(new TextPrompt(UserData.Extra));
            _dialogs.Add(new TextPrompt(UserData.Rating));
            _dialogs.Add(new TextPrompt(UserData.Feedback));
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
                    // The bot adapter encapsulates the authentication processes.
                    var botAdapter = (BotFrameworkAdapter)turnContext.Adapter;
                    await botAdapter.SignOutUserAsync(turnContext, _OAuthConnectionSettingName, cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync("You have been signed out.", cancellationToken: cancellationToken);
                    // TODO: Thank you for your time.. Make it pending for a new conversation.. 
                    // TODO: Check if the project is pending [forget me] or not ? 
//                    return;
                    await dialogContext.ReplaceDialogAsync(Auth, null, cancellationToken);
                }

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                //                if (!turnContext.Responded)
                if(results.Status == DialogTurnStatus.Empty)
                {
                    var userProfile = await _accessors.UserInfoAccessor.GetAsync(turnContext, () => new UserInfo(), cancellationToken);
                    var toBotFromAgent = IsReplyToUserMessage(turnContext) || false;

                    if (toBotFromAgent)
                    {
                        await dialogContext.BeginDialogAsync("replyToUser", null, cancellationToken);
                    }
                    else if (userProfile.State == UserDialogState.ProjectInOneOnOneConversation)
                    {
                        await dialogContext.BeginDialogAsync("replyToAgent", null, cancellationToken);
                    }
//                    else if (!turnContext.Responded)
//                    {
//                        await dialogContext.BeginDialogAsync(Start, null, cancellationToken);
//                    }
                    else
                    {
                        await dialogContext.BeginDialogAsync(Auth, null, cancellationToken);
                    }
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
                    await dialogContext.BeginDialogAsync(Auth, cancellationToken: cancellationToken); // Begin auth or start ?
                }
                else
                {
                    await dialogContext.BeginDialogAsync(Start, null, cancellationToken);
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected", cancellationToken: cancellationToken);
            }

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
                var client = GraphClient.GetAuthenticatedClient(tokenResponse.Token);
                var user = await GraphClient.GetMeAsync(client);
                await step.Context.SendActivityAsync($"Kon'nichiwa { user.DisplayName}! You are now logged in.", cancellationToken: cancellationToken);
                return await step.EndDialogAsync(cancellationToken);
            }

            await step.Context.SendActivityAsync("Login was not successful please try again. Aborting.", cancellationToken: cancellationToken);
//            return Dialog.EndOfTurn;
            return await step.ReplaceDialogAsync(Auth, null, cancellationToken);

        }

        /// <summary>
        /// Fetch the token and display it for the user if they asked to see it.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private static async Task<DialogTurnResult> GetTokenAgain(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            // Call the prompt again because we need the token. The reasons for this are:
            // 1. If the user is already logged in we do not need to store the token locally in the bot and worry
            // about refreshing it. We can always just call the prompt again to get the token.
            // 2. We never know how long it will take a user to respond. By the time the
            // user responds the token may have expired. The user would then be prompted to login again.
            //
            // There is no reason to store the token locally in the bot because we can always just call
            // the OAuth prompt to get the token or get a new token if needed.
            var prompt = await step.BeginDialogAsync(OAuthHelpers.LoginPromptDialogId, cancellationToken: cancellationToken);
            var tokenResponse = (TokenResponse)prompt.Result;
            if (tokenResponse != null)
            {
                await step.Context.SendActivityAsync($"Here is your token {tokenResponse.Token}", cancellationToken: cancellationToken);
            }

            await step.Context.SendActivityAsync("Login was not successful please try again. Aborting.", cancellationToken: cancellationToken);
            return Dialog.EndOfTurn;
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
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        $"Welcome to AuthenticationBot {member.Name}. {WelcomeText}",
                        cancellationToken: cancellationToken);
                }
            }
        }
        #endregion

        #region Introduction
        private async Task<DialogTurnResult> IntroductionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the users response is received.
            return await stepContext.PromptAsync(
                UserData.FirstStep, 
                CreateAdaptiveCardAsPrompt(cb.PresentationIntro()),
                cancellationToken);
        }
        private async Task<DialogTurnResult> PostIntroductionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context,
                () => new UserInfo(),
                cancellationToken);

            // Update the profile.
            userProfile.Introduction = (string)stepContext.Result;
            userProfile.State = UserDialogState.ProjectStarted;

            if (userProfile.Introduction.Equals(Constants.V2ShowExamples))
            {
                return await stepContext.ReplaceDialogAsync(ExamplePath, null, cancellationToken);
            }
            else if (userProfile.Introduction.Equals(Constants.V2LetsBegin))
            {
                return await stepContext.ReplaceDialogAsync(DetailPath, null, cancellationToken);
            }

            return null;
        }
        #endregion

        #region ExamplePath
        private async Task<DialogTurnResult> ShowExampleStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the users response is received.
            return await stepContext.PromptAsync(
                UserData.Style,
                CreateAdaptiveCardAsPrompt(cb.V2ShowExamples()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessExampleStep(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var userProfile = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);
            userProfile.State = UserDialogState.ProjectCollectingDetails;

            if (((string)stepContext.Result).Equals(Constants.V2LetsBegin))
            {
                return await stepContext.ReplaceDialogAsync(DetailPath, null, cancellationToken);
            }

            // Update the profile.
            userProfile.Style = (string)stepContext.Result;
            return await stepContext.ReplaceDialogAsync(PostSelectionPath, null, cancellationToken);
        }

        #endregion

        #region DetailPath

        private async Task<DialogTurnResult> PurposeStep (WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var userProfile = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);

            userProfile.Introduction = (string)stepContext.Result;
            userProfile.State = UserDialogState.ProjectCollectingDetails;

            return await stepContext.PromptAsync(
                UserData.Purpose,
                CreateAdaptiveCardAsPrompt(cb.V2PresentationPurpose()),
                cancellationToken);
        }
        private async Task<DialogTurnResult> ColorVariationStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);

            // Update the profile.
            userProfile.Purpose = (string)stepContext.Result;

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync(
                UserData.Color,
                CreateAdaptiveCardAsPrompt(cb.V2ColorVariations()),
                cancellationToken);
        }
        private async Task<DialogTurnResult> IllustrationStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);

            // Update the profile.
            userProfile.Color = (string)stepContext.Result;

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync(
                UserData.Visuals,
                CreateAdaptiveCardAsPrompt(cb.V2IllustrationsCard()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> PostDetailStep(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var userInfo =
                await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(),
                    cancellationToken);
            userInfo.Visuals = stepContext.Result as string;

            return await stepContext.ReplaceDialogAsync(PostSelectionPath, null, cancellationToken);
        }

        #endregion

        #region PostSelectionPath

        private async Task<DialogTurnResult> ImageOptions(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                UserData.Images,
                CreateAdaptiveCardAsPrompt(cb.V2ImageOptions()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> ExtraInfoStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);

            // Update the profile.
            userProfile.Images = (string)stepContext.Result;

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync(
                UserData.Extra,
                CreateAdaptiveCardAsPrompt(cb.AnythingElseCard()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> UserInfoAddedStep(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);

            // Update the profile.
            userProfile.Extra = (string)stepContext.Result;

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync(
                UserData.Extra,
                CreateAdaptiveCardAsPrompt(cb.ConfirmationCard()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStep (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync(
                UserData.Extra,
                CreateAdaptiveCardAsPrompt(cb.SummaryCard(userProfile)),
                cancellationToken);
            
        }
        private async Task<DialogTurnResult> TicketStep (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var message = stepContext.Context.Activity.Text;
            var currentUserProfile = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);

            if (message.Equals(Constants.ChangeSomething))
            {
                return currentUserProfile.Introduction.Equals(Constants.V2ShowExamples)
                    ? await stepContext.ReplaceDialogAsync(ExamplePath, null, cancellationToken)
                    : await stepContext.ReplaceDialogAsync(DetailPath, null, cancellationToken);
            }

            currentUserProfile.State = UserDialogState.ProjectInOneOnOneConversation;

            // TODO: create a card for the agent.
            var agentConversationId = await CreateAgentConversationMessage(stepContext.Context,
                $"PowerPoint request from {stepContext.Context.Activity.From.Name} via {stepContext.Context.Activity.ChannelId}",
                cb.V2VsoTicketCard(251,"https://www.microsoft.com"));

            // TODO: integrate VSO into this area
            await _endUserAndAgentIdMapping.CreateNewMapping("vsoTicket-251",
                stepContext.Context.Activity.From.Name,
                stepContext.Context.Activity.From.Id,
                JsonConvert.SerializeObject(stepContext.Context.Activity.GetConversationReference()),
                agentConversationId);

            await stepContext.Context.SendActivityAsync(
                CreateAdaptiveCardAsActivity(cb.V2VsoTicketCard(251, "https://www.microsoft.com")), cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> End(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            var userProfile =
                await _accessors.UserInfoAccessor.GetAsync(context.Context, () => new UserInfo(), cancellationToken);
            userProfile.State = UserDialogState.ProjectInOneOnOneConversation;

            return await context.EndDialogAsync(null, cancellationToken);
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

        #region Helpers
        private static PromptOptions CreateAdaptiveCardAsPrompt(AdaptiveCard card)
        {
            return new PromptOptions
            {
                Prompt = (Activity) MessageFactory.Attachment(CreateAdaptiveCardAttachment(card))
            };
        }
        private static Attachment CreateAdaptiveCardAttachment(AdaptiveCard card)
        {
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(card)),
            };
            return adaptiveCardAttachment;
        }

        private static IActivity CreateAdaptiveCardAsActivity(AdaptiveCard card)
        {
            return (Activity) MessageFactory.Attachment(CreateAdaptiveCardAttachment(card));
        }


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
                var channelData = new TeamsChannelData { Channel = agentChannelInfo };

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

        private bool IsReplyToUserMessage(ITurnContext context)
        {
            var isGroup = context.Activity.Conversation.IsGroup ?? false;
            var mentions = context.Activity.GetMentions();

            // TODO: mentions.length could crash if null!!!!
            return (isGroup && mentions.Length > 0 && mentions.FirstOrDefault().Text.Contains(_appSettings.BotName));
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


    public static class GraphClient
    {
        // Get information about the user.
        public static async Task<Microsoft.Graph.User> GetMeAsync(GraphServiceClient graphClient)
        {
            return await graphClient.Me.Request().GetAsync();
        }

        public static GraphServiceClient GetAuthenticatedClient(string token)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    requestMessage =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Local.Id + "\"");

                        return Task.CompletedTask;
                    }));
            return graphClient;
        }
    }
}
