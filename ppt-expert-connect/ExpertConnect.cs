// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using PPTExpertConnect.Helpers;
using PPTExpertConnect.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;

namespace PPTExpertConnect
{
    public class ExpertConnect : IBot
    {
        private const string BotName = "ecbot_tb";
        private const string AgentChannelName = "General2";

        private readonly string Start = "Start";
        private readonly string DetailPath = "Details";
        private readonly string ExamplePath = "Example";
        private readonly string PostSelectionPath = "PostSelection";

        private readonly BotAccessors _accessors;
        private readonly AzureBlobTranscriptStore _transcriptStore;
        private readonly IdTable _idTable;
        private readonly ILogger _logger;
        private DialogSet _dialogs;
        private CardBuilder cb;

        private readonly AppSettings _appSettings;
        private readonly SimpleCredentialProvider _botCredentials;

        public ExpertConnect(AzureBlobTranscriptStore transcriptStore, BotAccessors accessors, ILoggerFactory loggerFactory, IOptions<AppSettings> appSettings , IdTable idTable, ICredentialProvider credentials)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<ExpertConnect>();
            _logger.LogTrace("Turn start.");
            _accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));
            _transcriptStore = transcriptStore ?? throw new ArgumentNullException(nameof(transcriptStore)); // Test Mode ?
            _appSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
            _idTable = idTable ?? throw new ArgumentNullException(nameof(idTable));

            _botCredentials = (SimpleCredentialProvider)credentials ?? throw new ArgumentNullException(nameof(appSettings));
            
            cb = new CardBuilder(_appSettings);
            
            _dialogs = new DialogSet(accessors.DialogStateAccessor);

            var start = new WaterfallStep[]
            {
                IntroductionStep,
                PostIntroductionStep
            };

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
                End
            };

            _dialogs.Add(new WaterfallDialog(Start, start));
            _dialogs.Add(new WaterfallDialog(DetailPath, detailSteps));
            _dialogs.Add(new WaterfallDialog(ExamplePath, exampleSteps));
            _dialogs.Add(new WaterfallDialog(PostSelectionPath, postSelectionSteps));

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
            
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Run the DialogSet - let the framework identify the current state of the dialog from
                // the dialog stack and figure out what (if any) is the active dialog.
                var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                // If the DialogTurnStatus is Empty we should start a new dialog.
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(Start, null, cancellationToken);
                }
            } else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                bool isGroup = turnContext.Activity.Conversation.IsGroup ?? false;
                if (turnContext.Activity.ChannelId == "msteams" && isGroup) 
                {
                    await SaveAgentChannelIdInAzureStore(turnContext, _botCredentials);
                }
                await SaveBotIdInAzureStorage(turnContext, BotName);
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
            var userProfile = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);

            // Update the profile.
            userProfile.Introduction = (string)stepContext.Result;

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
        private async Task<DialogTurnResult> End (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            await _accessors.UserInfoAccessor.SetAsync(stepContext.Context, new UserInfo(), cancellationToken);

            // TODO: create a card for the agent.
            await CreateAgentConversationMessage(stepContext.Context,
                $"PowerPoint request from {stepContext.Context.Activity.From.Name} via {stepContext.Context.Activity.ChannelId}",
                cb.PresentationIntro());

            await stepContext.PromptAsync(
                UserData.Extra,
                CreateAdaptiveCardAsPrompt(cb.V2VsoTicketCard(
                    251,
                    "https://www.microsoft.com")),
                cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
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

                var ci = GetChannelId(connectorClient, context, AgentChannelName);
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

                //Trace.TraceInformation(
                //    $"[SUCCESS]: CreateAgentConversation. response id ={conversationResourceResponse.Id}");

                //WebApiConfig.TelemetryClient.TrackEvent("CreateAgentConversation", new Dictionary<string, string>
                //{
                //    {"endUser", agentMessage.From.Name},
                //    {"agentConversationId", conversationResourceResponse.Id},
                //});

                return conversationResourceResponse.Id;
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
                //WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                //{
                //    {"function", "CreateAgentConversation" }
                //});

                throw;
            }
        }

        #endregion
    }

}
