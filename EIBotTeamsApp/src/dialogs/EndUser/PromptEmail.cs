using System;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Sequence;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    [Serializable]
    public class PromptEmail : Prompt<string, string>
    {
        private const string EmailKey = "email";

        public PromptEmail(string prompt,
            string retry = null,
            string tooManyAttempts = null,
            int attempts = 2)
            : base(new PromptOptions<string>(prompt, retry, tooManyAttempts, attempts: attempts))
        {
        }

        protected override bool TryParse(IMessageActivity message, out string text)
        {
            text = message.Text;

            try
            {
                string email = GetEmail(text);
                if (!string.IsNullOrEmpty(email))
                {
                    text = email;
                    return true;
                }

                promptOptions.DefaultRetry =
                    $"I'm sorry, '{message.Text}' doesn't seem to be a valid Microsoft email address. Please retry.";
                return false;
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e);
                promptOptions.DefaultRetry =
                    $"I'm sorry, I couldn't understand '{message.Text}'. Please retry.";
                return false;
            }
        }

        string GetEmail(string text)
        {
            var entities = SequenceRecognizer.RecognizeEmail(text, Culture.English);
            //entities.Dump();
            foreach (var entity in entities)
            {
                if (entity.TypeName != EmailKey) break;
                if (entity.Resolution.TryGetValue("value", out object valueObject))
                {
                    string valueString = valueObject.ToString();
                    if (valueString.Contains("microsoft")) return valueObject.ToString();
                }
            }
            return "none";
        }
    }
}