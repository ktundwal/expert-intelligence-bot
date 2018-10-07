using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    public class TextDialog : IDialog<string>
    {
        private const int NumAttempts = 1;

        // "Okay, since this is your first freelancer request, can you please tell us your name?"
        private readonly string _questionToAsk;
        private readonly int _minCharacters;
        private readonly int _maxCharacters;

        public TextDialog(string questionToAsk, int minCharacters = 150, int maxCharacters = 1000)
        {
            _questionToAsk = questionToAsk;
            _minCharacters = minCharacters;
            _maxCharacters = maxCharacters;
        }
#pragma warning disable 1998
        public async Task StartAsync(IDialogContext context)
#pragma warning restore 1998
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var promptText = new PromptText(_questionToAsk,
                "Please try again", "Wrong again. Too many attempts.", 
                NumAttempts, _minCharacters, _maxCharacters);
            context.Call(promptText, OnAliasReceivedAsync);
        }

        private async Task OnAliasReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var userAnswer = await result;
            WebApiConfig.TelemetryClient.TrackEvent("TextDialog", new Dictionary<string, string>
            {
                {"name",  context.Activity.From.Name},
                {"questionToAsk", _questionToAsk},
                {"userAnswer", userAnswer }
            });

            context.Done(userAnswer);
        }
    }

    public class AliasDialog : IDialog<string>
    {
#pragma warning disable 1998
        public async Task StartAsync(IDialogContext context)
#pragma warning restore 1998
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var promptText = new PromptText(
                "Okay, since this is your first freelancer request, can you please tell us your Microsoft alias?",
                "Please try again", "Wrong again. Too many attempts.", 2, 2);
            context.Call(promptText, OnAliasReceivedAsync);
        }

        private async Task OnAliasReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var alias = await result;
            WebApiConfig.TelemetryClient.TrackEvent("PromptForAlias", new Dictionary<string, string>
            {
                {"name",  context.Activity.From.Name
                },
                {"alias", alias }
            });
            context.UserData.SetValue(UserProfileKeys.AliasKey, alias);

            context.Done(alias);
        }
    }
}