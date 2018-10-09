using System;
using System.Text.RegularExpressions;
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
        private const string PhoneNumberKey = "phonenumber";

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

            try
            {
                string phoneNumber = GetPhoneNumber(text);
                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    text = phoneNumber;
                    return true;
                }

                promptOptions.DefaultRetry =
                    $"I'm sorry, '{text}' doesn't seem to be a valid phone number. Please retry.";
                return false;
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e);
                promptOptions.DefaultRetry =
                    $"I'm sorry, I couldn't understand '{text}'. Please retry.";
                return false;
            }
        }

        string GetPhoneNumber(string text)
        {
            var entities = SequenceRecognizer.RecognizePhoneNumber(text, Culture.English);
            //entities.Dump();
            foreach (var entity in entities)
            {
                if (entity.TypeName != PhoneNumberKey) break;
                if (entity.Resolution.TryGetValue("score", out object scoreObject))
                {
                    double score = Convert.ToDouble(scoreObject);
                    if (score >= 0.4)
                    {
                        if (entity.Resolution.TryGetValue("value", out object valueObject))
                        {
                            return FormatPhoneNumber(valueObject);
                        }
                    }
                }
            }
            return "";
        }

        public static string FormatPhoneNumber(object valueObject)
        {
            return Regex.Replace(valueObject.ToString(), @"(?:\+1\D*)?([2-9]\d{2})\D*([2-9]\d{2})\D*(\d{4})", "+1-$1-$2-$3");
        }
    }
}