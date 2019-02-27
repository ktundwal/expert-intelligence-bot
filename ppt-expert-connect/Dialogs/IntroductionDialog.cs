using System;
using System.Linq;
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
        private readonly VsoHelper _vsoHelper;

        public IntroductionDialog(string id, CardBuilder cb, VsoHelper vso)
            : base(id)
        {
            InitialDialogId = InitialId;
            _cardBuilder = cb;
            _vsoHelper = vso;

            var steps = new WaterfallStep[] { IntroductionStep, PostIntroductionStep };
            AddDialog(new TextPrompt(TextPrompt, IntroductionOptionsValidatorAsync));
            AddDialog(new WaterfallDialog(InitialId, steps));
        }

        private async Task<DialogTurnResult> IntroductionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the users response is received.
            var userInfo = DialogHelper.GetUserInfoFromContext(stepContext);

            try
            {
                var vsoItem = await _vsoHelper.CreateTaskOnly(
                    stepContext.Context.Activity.ChannelId,
                    stepContext.Context.Activity.From.Id,
                    stepContext.Context.Activity.From.Name,
                    stepContext.Context.Activity.From.Name);
                userInfo.VsoId = vsoItem.id.ToString();
                userInfo.VsoLink = vsoItem.url;
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync($"Please wait for the agent to respond to open project **{userInfo.VsoId}**. Otherwise, use reset to close the existing project and open a new one.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync(userInfo, cancellationToken);
            }

            return await stepContext.PromptAsync(
                TextPrompt,
                DialogHelper.CreateAdaptiveCardAsPrompt(_cardBuilder.PresentationIntro()),
                cancellationToken).ConfigureAwait(false);
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

        private Task<bool> IntroductionOptionsValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var text = promptContext.Recognized.Value;
            string[] options = { Constants.V2ShowExamples, Constants.V2LetsBegin };
            return Task.FromResult(options.Contains(text, StringComparer.InvariantCultureIgnoreCase));
        }
    }
}
