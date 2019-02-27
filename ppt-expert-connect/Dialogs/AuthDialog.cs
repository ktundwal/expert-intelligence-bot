using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.ExpertConnect.Helpers;
using Microsoft.ExpertConnect.Models;

namespace Microsoft.ExpertConnect.Dialogs
{
    public class AuthDialog : ComponentDialog
    {
        private const string InitialId = DialogId.Auth;
        private const string DictionaryKey = nameof(AuthDialog);

        public AuthDialog(string id, string oAuthConnectionSettingName)
            : base(id)
        {
            InitialDialogId = InitialId;

            var steps = new WaterfallStep[] { PromptStepAsync, LoginStepAsync };
            AddDialog(OAuthHelpers.Prompt(oAuthConnectionSettingName));
            AddDialog(new WaterfallDialog(InitialId, steps));
        }

        private static async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            return await step.BeginDialogAsync(OAuthHelpers.LoginPromptDialogId, cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var userProfile = DialogHelper.GetUserInfoFromContext(step);
            var tokenResponse = (TokenResponse)step.Result;
            if (tokenResponse != null)
            {
                userProfile.Token = tokenResponse;
                await step.Context.SendActivityAsync(Constants.PostLoginWelcomeMessage, cancellationToken: cancellationToken);
                return await step.EndDialogAsync(userProfile, cancellationToken);
            }

            await step.Context.SendActivityAsync(
                "Login was not successful please try again. Starting Auth dialog back again.",
                cancellationToken: cancellationToken);
            return await step.ReplaceDialogAsync(DialogId.Auth, userProfile, cancellationToken);
        }
    }
}
