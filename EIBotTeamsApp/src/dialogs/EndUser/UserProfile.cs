using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    public static class UserProfile
    {
        private const string FriendlyNameKey = "FriendlyName";

        public static string GetFriendlyName(IDialogContext context, bool promptUserIfNotAvailable = true)
        {
            if (!context.UserData.TryGetValue(FriendlyNameKey, out string friendlyName))
            {
                // is this over SMS?
                if (!ActivityHelper.IsPhoneNumber(context.Activity.From.Name))
                {
                    // name is friendly name
                    context.UserData.SetValue(FriendlyNameKey, context.Activity.From.Name);
                }
                else
                {
                    if (promptUserIfNotAvailable) PromptForFriendlyNameAndSaveIt(context);
                    else friendlyName = string.Empty;
                }
            }
            return friendlyName;
        }

        private static void PromptForFriendlyNameAndSaveIt(IDialogContext context)
        {
            context.Call(new PromptText(
                    "I haven't seen you before. Please tell me your name",
                    "Please try again", "Wrong again. Too many attempts.", 2, 2),
                async delegate (IDialogContext dialogContext, IAwaitable<string> result)
                {
                    var name = await result;
                    WebApiConfig.TelemetryClient.TrackEvent("AskUserForFriendlyName", new Dictionary<string, string>
                    {
                        {"phoneNumber",  context.Activity.From.Name},
                        {"friendlyName", name }
                    });
                    context.UserData.SetValue(FriendlyNameKey, name);
                });
        }

        public static void EnsureWeKnowAboutUser(IDialogContext context)
        {
            // If this is over SMS, look up in our table if we have done this before
            //      if not, prompt for org alias => then query graph and get the name
            //      if yes, greet by name
            // If this is over teams, look up in our table if we have done this before
            //      If not, prompt for phone number
            //      greet by name
            throw new System.NotImplementedException();
        }
    }
}