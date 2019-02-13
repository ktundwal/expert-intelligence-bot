using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using com.microsoft.ExpertConnect.Helpers;
using com.microsoft.ExpertConnect.Models;

namespace com.microsoft.ExpertConnect.Dialogs
{
    public class ExampleTemplateDialog : ComponentDialog
    {
        private const string InitialId = DialogId.ExamplePath;
        private const string TextPrompt = "textPrompt";

        private readonly CardBuilder _cardBuilder;

        public ExampleTemplateDialog(string id, CardBuilder cb) : base(id)
        {
            InitialDialogId = InitialId;
            _cardBuilder = cb;

            var steps = new WaterfallStep[] { ShowExampleStep, ProcessExampleStep };
            AddDialog(new TextPrompt(TextPrompt));
            AddDialog(new WaterfallDialog(InitialId, steps));
        }

        #region ExamplePath
        private async Task<DialogTurnResult> ShowExampleStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the users response is received.
            return await stepContext.PromptAsync(
                TextPrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.V2ShowExamples()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessExampleStep(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);
            var userInput = (string) stepContext.Result;

            if (userInput.Equals(Constants.CreateBrief))
            {
                userInfo.State = UserDialogState.ProjectCollectTemplateDetails;
            }
            else
            {
                userInfo.Style = userInput;
                userInfo.State = UserDialogState.ProjectCollectDetails;
            }
            return await stepContext.EndDialogAsync(userInfo, cancellationToken);
        }

        #endregion

    }
}
