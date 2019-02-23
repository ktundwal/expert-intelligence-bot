using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.ExpertConnect.Helpers;
using Microsoft.ExpertConnect.Models;

namespace Microsoft.ExpertConnect.Dialogs
{
    public class TemplateDetailDialog : ComponentDialog
    {
        private const string InitialId = DialogId.DetailPath;
        private const string DictionaryKey = nameof(TemplateDetailDialog);
        private const string TextPrompt = "textPrompt";
        private const string PurposePrompt = "purposePrompt";
        private const string ColorPrompt = "colorPrompt";
        private const string IllustrationPrompt = "illustrationPrompt";

        private readonly CardBuilder _cardBuilder;

        public TemplateDetailDialog(string id, CardBuilder cb)
            : base(id)
        {
            InitialDialogId = InitialId;
            _cardBuilder = cb;

            var detailSteps = new WaterfallStep[]
            {
                PurposeStep,
                ColorVariationStep,
                IllustrationStep,
                PostDetailStep,
            };

            AddDialog(new TextPrompt(
                PurposePrompt,
                Helper.CreateValidatorFromOptionsAsync(new[]
                {
                    Constants.V2NewProject, Constants.V2NewProjectDesc, Constants.V2ProgressReport,
                    Constants.V2ProgressReportDesc,
                })));
            AddDialog(new TextPrompt(
                ColorPrompt,
                Helper.CreateValidatorFromOptionsAsync(new[]
                    {
                        Constants.ColorDark, Constants.ColorLight, Constants.Colorful, Constants.NoneOfThese,
                    })));
            AddDialog(new TextPrompt(
                IllustrationPrompt,
                Helper.CreateValidatorFromOptionsAsync(new[]
                {
                    Constants.VisualsPhotos, Constants.VisualsIllustrations, Constants.VisualsShapes,
                    Constants.NoneOfThese,
                })));
            AddDialog(new TextPrompt(TextPrompt));
            AddDialog(new WaterfallDialog(InitialId, detailSteps));
        }

        private async Task<DialogTurnResult> PurposeStep(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);

            userInfo.Introduction = Constants.V2LetsBegin;
            userInfo.State = UserDialogState.ProjectCollectingTemplateDetails;

            return await stepContext.PromptAsync(
                PurposePrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.V2PresentationPurpose()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> ColorVariationStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);

            // Update the profile.
            userInfo.Purpose = (string)stepContext.Result;
            userInfo.State = UserDialogState.ProjectCollectingTemplateDetails;

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync(
                ColorPrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.V2ColorVariations()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> IllustrationStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);

            // Update the profile.
            userInfo.Color = (string)stepContext.Result;
            userInfo.State = UserDialogState.ProjectCollectingTemplateDetails;

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync(
                IllustrationPrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.V2IllustrationsCard()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> PostDetailStep(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);
            userInfo.Visuals = stepContext.Result as string;
            userInfo.State = UserDialogState.ProjectCollectDetails;

            return await stepContext.EndDialogAsync(userInfo, cancellationToken);
        }
    }
}
