using System;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    [Serializable]
    public class PromptText : Prompt<string, string>
    {
        public PromptText(string prompt,
            string retry = null,
            string tooManyAttempts = null,
            int attempts = 3,
            int minLength = 200,
            int maxLength = 500)
            : base(new PromptOptions<string>(prompt, retry, tooManyAttempts, attempts: attempts))
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }

        protected override bool TryParse(IMessageActivity message, out string text)
        {
            text = message.Text;
            return IsValidDescription(message.Text);
        }

        private bool IsValidDescription(string text) => !string.IsNullOrWhiteSpace(text) && text.Length > MinLength && text.Length < MaxLength;

        private readonly int MinLength;
        private readonly int MaxLength;
    }
}