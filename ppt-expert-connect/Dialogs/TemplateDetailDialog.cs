using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using PPTExpertConnect.Helpers;
using PPTExpertConnect.Models;

namespace PPTExpertConnect.Dialogs
{
    public class TemplateDetailDialog : ComponentDialog
    {
        private const string InitialId = DialogId.DetailPath;
        private const string DictionaryKey = nameof(TemplateDetailDialog);
        private const string TextPrompt = "textPrompt";

        private readonly CardBuilder _cardBuilder;

        public TemplateDetailDialog(string id, CardBuilder cb) : base(id)
        {
            InitialDialogId = InitialId;
            _cardBuilder = cb;

            var detailSteps = new WaterfallStep[]
            {
                PurposeStep,
                ColorVariationStep,
                IllustrationStep,
                PostDetailStep
            };
            AddDialog(new TextPrompt(TextPrompt));
            AddDialog(new WaterfallDialog(InitialId, detailSteps));
        }

        #region DetailPath

        private async Task<DialogTurnResult> PurposeStep(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);

            userInfo.Introduction = Constants.V2LetsBegin;
            userInfo.State = UserDialogState.ProjectCollectingTemplateDetails;

            return await stepContext.PromptAsync(
                TextPrompt,
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
                TextPrompt,
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
                TextPrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.V2IllustrationsCard()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> PostDetailStep(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);
            userInfo.Visuals = stepContext.Result as string;
            userInfo.State = UserDialogState.ProjectCollectDetails;

            return await stepContext.EndDialogAsync(userInfo, cancellationToken);
        }

        #endregion
    }
}
