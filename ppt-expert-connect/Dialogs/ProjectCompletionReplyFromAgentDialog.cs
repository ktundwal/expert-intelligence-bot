using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.ExpertConnect.Helpers;
using Microsoft.ExpertConnect.Models;
using Microsoft.Recognizers.Text;
using Constants = Microsoft.ExpertConnect.Helpers.Constants;

namespace Microsoft.ExpertConnect.Dialogs
{

    public class ProjectCompletionReplyFromAgentDialog : ComponentDialog
    {
        private const string InitialId = DialogId.UserToSelectProjectStatePath;
        private const string DictionaryKey = nameof(ProjectCompletionReplyFromAgentDialog);
        private const string TextPrompt = "textPrompt";

        public ProjectCompletionReplyFromAgentDialog(string id)
            : base(id)
        {
            InitialDialogId = InitialId;

            var steps = new WaterfallStep[] { PostCompletionChoiceSelection };
            AddDialog(new TextPrompt(
                TextPrompt,
                Helper.CreateValidatorFromOptionsAsync(new[] {Constants.Complete, Constants.Revision})));

            AddDialog(new WaterfallDialog(InitialId, steps));
        }

        private async Task<DialogTurnResult> PostCompletionChoiceSelection(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var choice = stepContext.Context.Activity.Text;

            var dialogIdToStart = choice.Equals(Constants.Revision)
                ? DialogId.ProjectRevisionPath
                : DialogId.ProjectCompletePath;

            return await stepContext.ReplaceDialogAsync(
                dialogIdToStart,
                DialogHelper.GetUserInfoFromContext(stepContext),
                cancellationToken);
        }
    }

}
