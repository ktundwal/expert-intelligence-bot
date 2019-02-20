using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.ExpertConnect.Helpers;
using Microsoft.ExpertConnect.Models;
using Microsoft.Extensions.Configuration;

namespace Microsoft.ExpertConnect.Dialogs
{
    public class ProjectDetailDialog : ComponentDialog
    {
        private const string InitialId = DialogId.PostSelectionPath;
        private const string DictionaryKey = nameof(TemplateDetailDialog);
        private const string TextPrompt = "textPrompt";

        private readonly CardBuilder _cardBuilder;
        private readonly string _oAuthConnectionSettingName;
        private readonly string _shareFileWith;

        public ProjectDetailDialog(string id, CardBuilder cb, IConfiguration config)
            : base(id)
        {
            InitialDialogId = InitialId;
            _cardBuilder = cb;
            _oAuthConnectionSettingName = Helper.GetValueFromConfiguration(config, AppSettingsKey.OAuthConnectionSettingsName);
            _shareFileWith = Helper.GetValueFromConfiguration(config, AppSettingsKey.ShareFileWith);

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

        private async Task<DialogTurnResult> UserInfoAddedStep(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);

            // Update the profile.
            userInfo.Extra = (string)stepContext.Result;
            userInfo.State = UserDialogState.ProjectCollectingDetails;

            // TODO: Add File Into the OneDrive Folder
            var token = await ((BotFrameworkAdapter)stepContext.Context.Adapter)
                .GetUserTokenAsync(stepContext.Context, _oAuthConnectionSettingName, null, cancellationToken)
                .ConfigureAwait(false);
            if (token != null)
            {
                var driveItem = DialogHelper.UploadAnItemToOneDrive(token, userInfo.Style, _shareFileWith);
                // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
                return await stepContext.PromptAsync(
                TextPrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.ConfirmationCard(driveItem.WebUrl)),
                cancellationToken);
            }

            return await stepContext.PromptAsync(
                    TextPrompt,
                    DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.ConfirmationCard("UploadFailed")),
                    cancellationToken);
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
            }

            userInfo.State = UserDialogState.ProjectCreated;
//
//            await stepContext.Context.SendActivityAsync(
//                DialogHelper.CreateAdaptiveCardAsActivity(_cardBuilder.V2VsoTicketCard(251, "https://www.microsoft.com")), cancellationToken);

            return await stepContext.EndDialogAsync(userInfo, cancellationToken);
        }

        #endregion

    }
}
