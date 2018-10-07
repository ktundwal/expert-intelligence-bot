using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Office.EIBot.Service.utility;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    public static class UserProfileHelper
    {
        public const string UserProfileKey = "userProfile";

        public static string GetFriendlyName(IDialogContext context)
        {
            if (context.UserData.TryGetValue(UserProfileKey, out UserProfile userProfile))
            {
                if (!string.IsNullOrEmpty(userProfile.Name))
                    return userProfile.Name.Split(' ')[0];
                throw new System.Exception("User name isn't available");
            }
            throw new System.Exception("User name isn't available");
        }

        public static UserProfile GetUserProfile(IDialogContext context)
        {
            if (!context.UserData.TryGetValue(UserProfileKey, out UserProfile userProfile))
            {
                return userProfile;
            }
            throw new System.Exception("User profile isn't available");
        }
    }
}