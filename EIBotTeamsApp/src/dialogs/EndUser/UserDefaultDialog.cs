using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Office.EIBot.Service.utility;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    [Serializable]
    public class UserDefaultDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (await ConversationHelpers.RelayMessageToAgentIfThereIsAnOpenResearchProject(context))
            {
                // end the context
                context.Done<object>(null);
            }
            else
            {
                await context.PostWithRetryAsync("Sorry, I am not following you.");
                context.Call(new UserHelpDialog(), EndHelpDialog);
            }
        }

        public Task EndHelpDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done<object>(null);
            return Task.CompletedTask;
        }
    }
}