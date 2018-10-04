using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    public static class UserProfile
    {
        private const string FriendlyNameKey = "FriendlyName";
        private const string AliasKey = "Alias";
        private const string NameKey = "Name";
        private const string PhoneKey = "Phone";

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

        public static async Task<utility.UserProfile> GetUserProfileFromStoreOrAskFromUser(IDialogContext context)
        {
            var userTable = new UserTable();
            var botUsers = await userTable.GetUserByChannelSpecificId(context.Activity.ChannelId, context.Activity.From.Id);
            if (botUsers.Length == 0)
            {
                switch (context.Activity.ChannelId)
                {
                    case ActivityHelper.SmsChannelId:
                        PromptForNameAndPhoneNumber(context);
                        return await userTable.AddUser(
                            context.Activity.ChannelId,
                            context.Activity.From.Id,
                            context.UserData.GetValue<string>(NameKey),
                            context.Activity.From.Id,   // if SMS, id is same as phone
                            context.UserData.GetValue<string>(AliasKey));
                    case ActivityHelper.MsTeamChannelId:
                        PromptForAliasAndPhoneNumber(context);
                        return await userTable.AddUser(
                            context.Activity.ChannelId,
                            context.Activity.From.Id,
                            context.Activity.From.Name, 
                            context.UserData.GetValue<string>(PhoneKey),
                            context.UserData.GetValue<string>(AliasKey));
                    default:
                        throw new System.Exception("Unsupported channel");
                }
            }

            return botUsers.First();
        }

        private static void PromptForAliasAndPhoneNumber(IDialogContext context)
        {
            context.Call(new PromptText(
                    "Okay, since this is your first freelancer request, can you please tell us your Microsoft alias?" +
                    "That way you’ll also be able to chat with us via SMS text messages.",
                    "Please try again", "Wrong again. Too many attempts.", 2, 2),
                async delegate (IDialogContext aliasDialogContext, IAwaitable<string> aliasResult)
                {
                    var alias = await aliasResult;
                    WebApiConfig.TelemetryClient.TrackEvent("PromptForAlias", new Dictionary<string, string>
                    {
                        {"name",  context.Activity.From.Name},
                        {"alias", alias }
                    });
                    context.UserData.SetValue(AliasKey, alias);
                    context.Call(new PromptText(
                            "Can you also please tell us your phone number?  That way you’ll also be able to chat with us via SMS text messages.",
                            "Please try again", "Wrong again. Too many attempts.", 2, 10),
                        async delegate (IDialogContext phoneDialogContext, IAwaitable<string> phoneResult)
                        {
                            var phone = await phoneResult;
                            WebApiConfig.TelemetryClient.TrackEvent("PromptForPhone", new Dictionary<string, string>
                            {
                                {"name",  context.Activity.From.Name},
                                {"phone", phone }
                            });
                            context.UserData.SetValue(PhoneKey, phone);
                        });
                });
        }

        private static void PromptForNameAndPhoneNumber(IDialogContext context)
        {
            context.Call(new PromptText(
                    "Okay, since this is your first freelancer request, can you please tell us your name? " +
                    "That way you’ll also be able to chat with us via SMS text messages.",
                    "Please try again", "Wrong again. Too many attempts.", 2, 2),
                async delegate (IDialogContext aliasDialogContext, IAwaitable<string> nameResult)
                {
                    var name = await nameResult;
                    WebApiConfig.TelemetryClient.TrackEvent("PromptForName", new Dictionary<string, string>
                    {
                        {"id",  context.Activity.From.Id},
                        {"alias", name }
                    });
                    context.UserData.SetValue(NameKey, name);
                    context.Call(new PromptText(
                            "Can you also please tell us your phone number?  That way you’ll also be able to chat with us via SMS text messages.",
                            "Please try again", "Wrong again. Too many attempts.", 2, 10),
                        async delegate (IDialogContext phoneDialogContext, IAwaitable<string> phoneResult)
                        {
                            var phone = await phoneResult;
                            WebApiConfig.TelemetryClient.TrackEvent("PromptForPhone", new Dictionary<string, string>
                            {
                                {"name",  context.Activity.From.Name},
                                {"phone", phone }
                            });
                            context.UserData.SetValue(PhoneKey, phone);
                        });
                });
        }
    }
}