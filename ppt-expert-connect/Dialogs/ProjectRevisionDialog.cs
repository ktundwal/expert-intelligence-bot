using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.ExpertConnect.Helpers;
using Microsoft.ExpertConnect.Models;

namespace Microsoft.ExpertConnect.Dialogs
{
    public class ProjectRevisionDialog : ComponentDialog
    {
        private const string InitialId = DialogId.ProjectCompletePath;
        private const string TextPrompt = "textPrompt";

        private readonly CardBuilder _cardBuilder;

        public ProjectRevisionDialog(string id, CardBuilder cb)
            : base(id)
        {
            InitialDialogId = InitialId;
            _cardBuilder = cb;

            var steps = new WaterfallStep[] {PromptForRevisionFeedbackStep, ProcessRevisionFeedback};
            AddDialog(new TextPrompt(TextPrompt));
            AddDialog(new WaterfallDialog(InitialId, steps));
        }

        private async Task<DialogTurnResult> PromptForRevisionFeedbackStep(
            WaterfallStepContext context,
            CancellationToken cancellationToken)
        {
            return await context.PromptAsync(
                TextPrompt, 
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.V2AskForRevisionChanges()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessRevisionFeedback(
            WaterfallStepContext context,
            CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userInfo = DialogHelper.GetUserInfoFromContext(context);

            // Update the profile.
            userInfo.Feedback += (string)context.Result;
            userInfo.State = UserDialogState.ProjectUnderRevision;
            return await context.EndDialogAsync(userInfo, cancellationToken);
        }
    }
}
