using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using PPTExpertConnect.Helpers;
using PPTExpertConnect.Models;

namespace PPTExpertConnect.Dialogs
{
    public class ProjectDetailDialog : ComponentDialog
    {
        private const string InitialId = DialogId.PostSelectionPath;
        private const string DictionaryKey = nameof(TemplateDetailDialog);
        private const string TextPrompt = "textPrompt";

        private readonly CardBuilder _cardBuilder;

        public ProjectDetailDialog(string id, CardBuilder cb) : base(id)
        {
            InitialDialogId = InitialId;
            _cardBuilder = cb;

            var steps = new WaterfallStep[]
            {
                ImageOptions,
                ExtraInfoStep,
                UserInfoAddedStep,
                SummaryStep,
                TicketStep
            };
            AddDialog(new TextPrompt(TextPrompt));
            AddDialog(new WaterfallDialog(InitialId, steps));
        }

        #region PostSelectionPath

        private async Task<DialogTurnResult> ImageOptions(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                TextPrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.V2ImageOptions()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> ExtraInfoStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);

            // Update the profile.
            userInfo.Images = (string)stepContext.Result;
            userInfo.State = UserDialogState.ProjectCollectingDetails;

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync(
                TextPrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.AnythingElseCard()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> UserInfoAddedStep(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);

            // Update the profile.
            userInfo.Extra = (string)stepContext.Result;
            userInfo.State = UserDialogState.ProjectCollectingDetails;

            // TODO: Add File Into the OneDrive Folder
            //            var token = await ((BotFrameworkAdapter)stepContext.Context.Adapter)
            //                .GetUserTokenAsync(stepContext.Context, _OAuthConnectionSettingName, null, cancellationToken)
            //                .ConfigureAwait(false);
            //            if (token != null)
            //            {
            //                var driveItem = UploadAnItemToOneDrive(token);
            //                // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            //                return await stepContext.PromptAsync(
            //                TextPrompt,
            //                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.ConfirmationCard(driveItem.WebUrl)),
            //                cancellationToken);
            //            }
            //            else
            //            {
            return await stepContext.PromptAsync(
                    TextPrompt,
                    DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.ConfirmationCard("http://www.microsoft.com")),
                    cancellationToken);
//            }

        }

        private async Task<DialogTurnResult> SummaryStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync(
                TextPrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.SummaryCard(userInfo)),
                cancellationToken);

        }
        private async Task<DialogTurnResult> TicketStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);
            
            var message = stepContext.Context.Activity.Text;

            if (message.Equals(Constants.ChangeSomething))
            {
                userInfo.State = userInfo.Introduction.Equals(Constants.V2ShowExamples)
                    ? UserDialogState.ProjectSelectExampleOptions
                    : UserDialogState.ProjectCollectTemplateDetails;

                return await stepContext.EndDialogAsync(userInfo, cancellationToken);
//                    ? await stepContext.ReplaceDialogAsync(ExamplePath, null, cancellationToken)
//                    : await stepContext.ReplaceDialogAsync(DetailPath, null, cancellationToken);
            }

            userInfo.State = UserDialogState.ProjectCreated;

            // TODO: Create a VSO ticket here.

            // TODO: create a card for the agent.
//            var agentConversationId = await CreateAgentConversationMessage(stepContext.Context,
//                $"PowerPoint request from {stepContext.Context.Activity.From.Name} via {stepContext.Context.Activity.ChannelId}",
//                _cardBuilder.V2VsoTicketCard(251, "https://www.microsoft.com"));
//
//            // TODO: integrate VSO into this area
//            await _endUserAndAgentIdMapping.CreateNewMapping("vsoTicket-251",
//                stepContext.Context.Activity.From.Name,
//                stepContext.Context.Activity.From.Id,
//                JsonConvert.SerializeObject(stepContext.Context.Activity.GetConversationReference()),
//                agentConversationId);

            await stepContext.Context.SendActivityAsync(
                DialogHelper.CreateAdaptiveCardAsActivity(_cardBuilder.V2VsoTicketCard(251, "https://www.microsoft.com")), cancellationToken);

            return await stepContext.EndDialogAsync(userInfo, cancellationToken);
        }

        #endregion

    }
}
