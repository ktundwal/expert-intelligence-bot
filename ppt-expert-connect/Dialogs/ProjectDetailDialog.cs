using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.ExpertConnect.Helpers;
using Microsoft.ExpertConnect.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.ExpertConnect.Dialogs
{
    public class ProjectDetailDialog : ComponentDialog
    {
        private readonly ILogger _logger;
        private const string InitialId = DialogId.PostSelectionPath;
        private const string DictionaryKey = nameof(TemplateDetailDialog);
        private const string ImagePrompt = "imagePrompt";
        private const string TextPrompt = "textPrompt";
        private const string TicketPrompt = "ticketPrompt";

        private readonly CardBuilder _cardBuilder;
        private readonly string _oAuthConnectionSettingName;
        private readonly string _shareFileWith;
        private readonly IConfiguration _config;
        private readonly IHostingEnvironment _hostingEnvironment;

        public ProjectDetailDialog(string id, CardBuilder cb, IConfiguration config, ILoggerFactory loggerFactory, IHostingEnvironment hosting)
            : base(id)
        {
            _logger = loggerFactory.CreateLogger<ProjectDetailDialog>();

            InitialDialogId = InitialId;
            _cardBuilder = cb;
            _config = config;
            _hostingEnvironment = hosting;
            _oAuthConnectionSettingName = Helper.GetValueFromConfiguration(config, AppSettingsKey.OAuthConnectionSettingsName);
            _shareFileWith = Helper.GetValueFromConfiguration(config, AppSettingsKey.ShareFileWith);

            var steps = new WaterfallStep[]
            {
                ImageOptions,
                ExtraInfoStep,
                UserInfoAddedStep,
                SummaryStep,
                TicketStep,
            };
            AddDialog(new TextPrompt(ImagePrompt, Helper.CreateValidatorFromOptionsAsync(new[] { Constants.NewImages, Constants.OwnImages })));
            AddDialog(new TextPrompt(TicketPrompt, Helper.CreateValidatorFromOptionsAsync(new[] { Constants.LooksGood, Constants.ChangeSomething })));
            AddDialog(new TextPrompt(TextPrompt));
            AddDialog(new WaterfallDialog(InitialId, steps));
        }

        private async Task<DialogTurnResult> ImageOptions(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                ImagePrompt,
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
                var styleLink = Helper.GetPowerPointTemplateLink(userInfo.Style, _config, _hostingEnvironment);
                styleLink = styleLink == string.Empty
                    ? Helper.GetPowerPointTemplateLink(userInfo.Color, _config, _hostingEnvironment)
                    : styleLink;
                var driveItem = DialogHelper.UploadAnItemToOneDrive(token, styleLink, _shareFileWith, _logger, userInfo.VsoId);
                userInfo.PptWebUrl = driveItem.WebUrl;
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

            return await stepContext.PromptAsync(
                TicketPrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.SummaryCard(userInfo)),
                cancellationToken);

        }

        private async Task<DialogTurnResult> TicketStep(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
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
            return await stepContext.EndDialogAsync(userInfo, cancellationToken);
        }
    }
}
