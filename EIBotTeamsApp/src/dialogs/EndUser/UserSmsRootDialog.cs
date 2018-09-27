using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Scorables;
using Microsoft.Bot.Connector;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    /// <summary>
    /// This is Root Dialog, its a triggring point for every Child dialog based on the RexEx Match with user input command
    /// </summary>

    [Serializable]
    public class UserSmsRootDialog : DispatchDialog
    {

        #region Default Dialog

        [MethodBind]
        [ScorableGroup(1)]
        public void SmsDefault(IDialogContext context, IActivity activity)
        {
            WebApiConfig.TelemetryClient.TrackEvent("UserSmsRootDialog.SmsDefault");
            context.Call(new UserSmsDefaultDialog(), this.EndDefaultDialog);
        }

        public Task EndDefaultDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done<object>(null);
            return Task.CompletedTask;
        }

        #endregion
    }
}