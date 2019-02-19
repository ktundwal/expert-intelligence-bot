using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.ExpertConnect.Helpers;
using Microsoft.ExpertConnect.Models;

namespace Microsoft.ExpertConnect.Dialogs
{
    public class IntroductionDialog : ComponentDialog
    {
        private const string InitialId = DialogId.Start;
        private const string TextPrompt = "textPrompt";

        private readonly CardBuilder _cardBuilder;

        public IntroductionDialog(string id, CardBuilder cb) : base(id)
        {
            InitialDialogId = InitialId;
            _cardBuilder = cb;

            var steps = new WaterfallStep[] { IntroductionStep, PostIntroductionStep };
            AddDialog(new TextPrompt(TextPrompt));
            AddDialog(new WaterfallDialog(InitialId, steps));
        }

        #region Introduction
        private async Task<DialogTurnResult> IntroductionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the users response is received.
            return await stepContext.PromptAsync(
                TextPrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.PresentationIntro()),
                cancellationToken);
        }

        private async Task<DialogTurnResult> PostIntroductionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);

            userInfo.Introduction = (string)stepContext.Result;
            userInfo.State = UserDialogState.ProjectStarted;

            if (userInfo.Introduction.Equals(Constants.V2ShowExamples))
            {
                userInfo.State = UserDialogState.ProjectSelectExampleOptions;
            }
            else if (userInfo.Introduction.Equals(Constants.V2LetsBegin))
            {
                userInfo.State = UserDialogState.ProjectCollectTemplateDetails;
            }

            return await stepContext.EndDialogAsync(userInfo, cancellationToken);
        }
        #endregion

    }
}
