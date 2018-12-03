using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Office.EIBot.Service.utility;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    /// <summary>
    /// This is Begin Dialog Class. Main purpose of this class is to notify users that Child dialog has been called 
    /// and its a Basic example to call Child dialog from Root Dialog.
    /// </summary>

    [Serializable]
    public class PresentationDesignDialog : IDialog<object>
    {
        private const string DescriptionKey = "description";
        private const string AliasKey = "alias";
        private const string VsoIdKey = "VsoId";
        private const string EndUserConversationIdKey = "EndUserConversationId";

        private int _minHoursToDesignPresentation;
        static readonly string Uri = ConfigurationManager.AppSettings["VsoOrgUrl"];
        static readonly string Project = ConfigurationManager.AppSettings["VsoProject"];

        public async Task StartAsync(IDialogContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Activity.ChannelId == ActivityHelper.MsTeamChannelId &&
                context.Activity.Conversation.ConversationType == ActivityHelper.ChatTypeIsPersonal)
            {
                _minHoursToDesignPresentation = Convert.ToInt32(ConfigurationManager.AppSettings["ResearchProjectViaTeamsMinHours"]);

                await context.PostWithRetryAsync("This is under development");
            }
            throw new System.Exception("PPT options is only available via MSTeams and 1:1 chat");
        }
    }
}