using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Office.EIBot.Service.dialogs.EndUser;
using Microsoft.Office.EIBot.Service.utility;

namespace Microsoft.Office.EIBot.Service.dialogs.Agent
{

    /// <summary>
    /// This is Help Dialog Class. Main purpose of this dialog class is to post the help commands in Teams.
    /// These are Actionable help commands for easy to use.
    /// </summary>
    [Serializable]
    public class ReplyToUserDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var activity = (IMessageActivity)context.Activity;

            EndUserAndAgentConversationMappingState mappingState =
                await VsoHelper.GetStateFromVsoGivenAgentConversationId(activity.Conversation.Id);

            if (ActivityHelper.HasAttachment(activity))
            {
                await context.PostWithRetryAsync(
                    $"Sending file attachments to user is not supported. " +
                    $"Please send it via SharePoint > Share > Email. Email is in VSO ticket");
            }
            else
            {
                await ActivityHelper.SendMessageToUserEx((IMessageActivity)context.Activity,
                    mappingState.EndUserName,
                    mappingState.EndUserId,
                    activity.Text.Replace(DialogMatches.ReplyToUser + " ", ""),
                    mappingState.VsoId);
            }

            await OnlineStatus.SetMemberActive(
                context.Activity.From.Name,
                context.Activity.From.Id,
                OnlineStatus.AgentMemberType);

            context.Done<object>(null);
        }
    }
}