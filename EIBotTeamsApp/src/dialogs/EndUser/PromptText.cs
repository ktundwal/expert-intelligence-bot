using System;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    [Serializable]
    public class PromptText : Prompt<string, string>
    {
        private readonly int _minLength;
        private readonly int _maxLength;

        public PromptText(string prompt,
            string retry = null,
            string tooManyAttempts = null,
            int attempts = 3,
            int minLength = 200,
            int maxLength = 500)
            : base(new PromptOptions<string>(prompt, retry, tooManyAttempts, attempts: attempts))
        {
            _minLength = minLength;
            _maxLength = maxLength;
        }

        protected override bool TryParse(IMessageActivity message, out string text)
        {
            text = message.Text;
            return IsValidDescription(message.Text);
        }

        private bool IsValidDescription(string text) => text.Length >= _minLength && !string.IsNullOrWhiteSpace(text) && text.Length < _maxLength;
    }
}