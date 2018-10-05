using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Scorables;
using Microsoft.Bot.Connector;
using Microsoft.Office.EIBot.Service.utility;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    /// <summary>
    /// This is Root Dialog for user
    /// </summary>

    [Serializable]
    public class UserRootDialog : DispatchDialog
    {
        #region Internet research Pattern

        [RegexPattern(DialogMatches.PerformInternetResearchMatch)]
        [ScorableGroup(1)]
        public void PerformInternetResearch(IDialogContext context, IActivity activity)
        {
            context.Call(new InternetResearchDialog(), EndInternetResearchDialog);
        }

        public async Task EndInternetResearchDialog(IDialogContext context, IAwaitable<bool> awaitable)
        {
            await context.PostWithRetryAsync("Have a nice day!");
            context.Done<object>(null);
        }

        #endregion

        #region Hello Dialog

        [MethodBind]
        [ScorableGroup(1)]
        public void RunHelloDialog(IDialogContext context, IActivity activity)
        {
            // introduce the bot
            context.Call(new UserProfileDialog(),
                async delegate(IDialogContext phoneDialogContext, IAwaitable<UserProfile> userProfileResult)
                {
                    UserProfile userProfile = await userProfileResult;
                    context.UserData.SetValue(UserProfileHelper.UserProfileKey, userProfile);
                    context.Call(new HelloDialog(), EndHelloDialog);
                }
            );
        }

        #endregion

        #region Help Dialog

        //[RegexPattern(DialogMatches.Help)]
        //[ScorableGroup(1)]
        //public void Help(IDialogContext context, IActivity activity)
        //{
        //    context.Call(new UserHelpDialog(), this.EndHelpDialog);
        //}

        //[MethodBind]
        //[ScorableGroup(2)]
        //public void Default(IDialogContext context, IActivity activity)
        //{
        //    context.Call(new UserDefaultDialog(), this.EndDefaultDialog);
        //}

        public Task EndHelpDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done<object>(null);
            return Task.CompletedTask;
        }

        public Task EndDefaultDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done<object>(null);
            return Task.CompletedTask;
        }

        public Task EndHelloDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done<object>(null);
            return Task.CompletedTask;
        }

        #endregion
    }
}