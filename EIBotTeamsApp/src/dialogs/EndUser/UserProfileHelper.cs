using System.Linq;
using System.Threading.Tasks;
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

        public static async Task<UserProfile> GetUserProfile(IDialogContext context)
        {
            if (context.UserData.TryGetValue(UserProfileKey, out UserProfile userProfile))
            {
                return userProfile;
            }
            // there is an open project, we know about the user. make sure we have userprofile set in userdata
            var userTable = new UserTable();
            var userProfileFromAzureStore = context.Activity.ChannelId == ActivityHelper.SmsChannelId
                ? await userTable.GetUserByMobilePhone(context.Activity.From.Id)
                : await userTable.GetUserByName(context.Activity.From.Name);

            var profileFromAzureStore = userProfileFromAzureStore as UserProfile[] ?? userProfileFromAzureStore.ToArray();
            if (profileFromAzureStore.Any())
            {
                return profileFromAzureStore.First();
            }

            throw new System.Exception("User profile isn't available");
        }
    }
}