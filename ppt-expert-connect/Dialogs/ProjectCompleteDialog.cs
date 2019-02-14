using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.ExpertConnect.Helpers;
using Microsoft.ExpertConnect.Models;

namespace Microsoft.ExpertConnect.Dialogs
{
    public class ProjectCompleteDialog : ComponentDialog
    {
        private const string InitialId = DialogId.ProjectCompletePath;
        private const string DictionaryKey = nameof(TemplateDetailDialog);
        private const string TextPrompt = "textPrompt";

        private readonly CardBuilder _cardBuilder;

        public ProjectCompleteDialog(string id, CardBuilder cb) : base(id)
        {
            InitialDialogId = InitialId;
            _cardBuilder = cb;

            var steps = new WaterfallStep[] {PromptForRatingsStep, ProcessRatingsStep, ProcessFeedback};
            AddDialog(new TextPrompt(TextPrompt));
            AddDialog(new WaterfallDialog(InitialId, steps));
        }

        #region ProjectComplete

        private async Task<DialogTurnResult> PromptForRatingsStep(WaterfallStepContext context,
            CancellationToken cancellationToken)
        {
            return await context.PromptAsync(
                TextPrompt, 
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.V2Ratings()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessRatingsStep(WaterfallStepContext context,
            CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userInfo = DialogHelper.GetUserInfoFromContext(context);

            // Update the profile.
            if (Int32.TryParse((string) context.Result, out var ratingValue))
            {
                userInfo.Rating = ratingValue;
                if (ratingValue <= 3)
                {
                    return await context.PromptAsync(
                        TextPrompt,
                        DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.V2Feedback(false, true)),
                        cancellationToken);
                }

                await DialogHelper.PostLearningContentAsync(context.Context, _cardBuilder, cancellationToken);
                return await context.PromptAsync(
                    TextPrompt,
                    DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.V2Feedback(false, false)),
                    cancellationToken);
            }

            return null;
        }

        private async Task<DialogTurnResult> ProcessFeedback(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);

            // Update the profile.
            userInfo.Feedback = (string) stepContext.Result;
            userInfo.State = UserDialogState.ProjectCompleted;

            if (userInfo.Rating <= 3)
            {
                await DialogHelper.PostLearningContentAsync(stepContext.Context, _cardBuilder, cancellationToken);
            }

            return await stepContext.EndDialogAsync(userInfo, cancellationToken);
        }

        #endregion

    }
}
