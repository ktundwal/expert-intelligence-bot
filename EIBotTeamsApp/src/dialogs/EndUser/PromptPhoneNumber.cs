using System;
using System.Linq;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Sequence;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    [Serializable]
    public class PromptPhoneNumber : Prompt<string, string>
    {
        public PromptPhoneNumber(string prompt,
            string retry = null,
            string tooManyAttempts = null,
            int attempts = 2)
            : base(new PromptOptions<string>(prompt, retry, tooManyAttempts, attempts: attempts))
        {
        }

        protected override bool TryParse(IMessageActivity message, out string text)
        {
            text = message.Text;

            var entities = SequenceRecognizer.RecognizePhoneNumber(text, Culture.English);
            return IsValidDescription(message.Text);
        }

        private bool IsValidDescription(string text) => SequenceRecognizer.RecognizePhoneNumber(text, Culture.English).Any();
    }
}