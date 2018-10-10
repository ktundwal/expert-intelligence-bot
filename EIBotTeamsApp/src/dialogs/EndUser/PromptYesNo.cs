using System;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Choice;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    [Serializable]
    public class PromptYesNo : Prompt<bool, bool>
    {
        public PromptYesNo(string prompt,
            string retry = null,
            string tooManyAttempts = null,
            int attempts = 2)
            : base(new PromptOptions<bool>(prompt, retry, tooManyAttempts, attempts: attempts))
        {
        }

        protected override bool TryParse(IMessageActivity message, out bool result)
        {
            var text = message.Text.Trim().ToLower().Replace("'","");

            try
            {
                result = GetBool(text);
                return true;
            }
            catch (System.Exception)
            {
                promptOptions.DefaultRetry =
                    $"I'm sorry, '{text}' doesn't seem to be a valid response. Please say 'yes' or 'no'";
                result = false;
                return false;
            }
        }

        bool GetBool(string text)
        {
            var entities = ChoiceRecognizer.RecognizeBoolean(text, Culture.English); ;
            foreach (var entity in entities)
            {
                if (entity.TypeName != "boolean") break;
                if (entity.Resolution.TryGetValue("score", out object scoreObject))
                {
                    double score = Convert.ToDouble(scoreObject);
                    if (score >= 0.5)
                    {
                        if (entity.Resolution.TryGetValue("value", out object valuObject))
                        {
                            return (bool)valuObject;
                        }
                    }
                }
            }
            throw new System.Exception("not a valid response");
        }

    }
}