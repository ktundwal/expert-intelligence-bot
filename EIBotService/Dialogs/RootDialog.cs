using System;
using System.Threading.Tasks;
using EIBot.CommandHandling;
using EIBot.Strings;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace EIBot.Dialogs
{
    /// <summary>
    /// Simple dialog that will only ever provide simple instructions.
    /// </summary>
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext dialogContext)
        {
            dialogContext.Wait(OnMessageReceivedAsync);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Responds back to the sender with the simple instructions.
        /// </summary>
        /// <param name="dialogContext">The dialog context.</param>
        /// <param name="result">The result containing the message sent by the user.</param>
        private async Task OnMessageReceivedAsync(IDialogContext dialogContext, IAwaitable<IMessageActivity> result)
        {
            IMessageActivity messageActivity = await result;
            string messageText = messageActivity.Text;

            if (!string.IsNullOrEmpty(messageText))
            {
                messageActivity = dialogContext.MakeMessage();

                messageActivity.Text =
                    $"<p>Hello! I am <b>Expert Intelligence Bot</b>. <br/><br/>I currently support 2 capabilities – Internet Research and Powerpoint Improvements. </p>" +
                    $"<ul><li>For <b>internet research</b> just let me know the topic, analysis or datapoints that you’re looking for and I’ll get you connected with a research expert who will clarify the research (if needed) " +
                    $"and send you the results (typically 5-10 web links and a summary). </li> " +
                    $"<li>For <b>Powerpoint</b> I can apply visual cleanup to make slides consistent, " +
                    $"and I can also provide some great design tips from a professional powerpoint designer. " +
                    $"You’ll receive an updated powerpoint file with all my changes (we can update 5-10 slides with a summary of the changes we made). </li></ul>" +
                    $"<p>Type '<b>human</b>' to be connected right away. (I have yet to onboard onto LUIS)</p>";
                    //$"* {string.Format(ConversationText.OptionsCommandHint, $"{Commands.CommandKeyword} {Commands.CommandListOptions}")}"
                    //+ $"\n\r* {string.Format(ConversationText.ConnectRequestCommandHint, Commands.CommandRequestConnection)}";

                await dialogContext.PostAsync(messageActivity);
            }

            dialogContext.Wait(OnMessageReceivedAsync);
        }
    }
}
