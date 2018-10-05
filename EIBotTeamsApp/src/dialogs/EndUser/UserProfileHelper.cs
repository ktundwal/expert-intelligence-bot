using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Office.EIBot.Service.utility;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    public static class UserProfileHelper
    {
        public const string UserProfileKey = "userProfile";

        public static string GetFriendlyName(IDialogContext context, bool promptUserIfNotAvailable = false)
        {
            if (!context.UserData.TryGetValue(UserProfileKey, out UserProfile userProfile))
            {
                return !string.IsNullOrEmpty(userProfile.Name) ? userProfile.Name.Split(' ')[0] : "";
            }
            return "";
        }

        public static UserProfile GetUserProfile(IDialogContext context)
        {
            if (!context.UserData.TryGetValue(UserProfileKey, out UserProfile userProfile))
            {
                return userProfile;
            }
            return null;
        }
    }
}