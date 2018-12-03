using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Office.EIBot.Service.Properties;
using Microsoft.Office.EIBot.Service.utility;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    [Serializable]
    public class PresentationDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context,
                CardBuilder.PresentationIntro()));
            context.Wait(this.MessageReceivedAsyncToPurposeSelection);

        }

        private async Task MessageReceivedAsyncToPurposeSelection(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var activity = await result;

            if (activity.Text == PresentationDialogStrings.LetsBegin.ToLower())
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationPurposeOptions()));
                context.Wait(this.MessageReceivedAsyncToStyleSelection);
            }
        }

        private async Task MessageReceivedAsyncToStyleSelection(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var activity = await result;

            if(activity.Text == PresentationDialogStrings.NewProject.ToLower())
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationStyleCard(activity.Text)));
                context.Wait(this.MessageReceivedAsyncFromStyleSelection);
            }
        }

        private async Task MessageReceivedAsyncFromStyleSelection(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var activity = await result;

            if (activity.Text == "modern")
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationStyleCard(activity.Text)));
                context.Wait(this.MessageReceivedAsyncToStyleSelection);
            }
        }

        private static void ThrowExceptionIfResultIsNull(IAwaitable<object> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException(nameof(result) + Strings.NullException);
            }
        }
    }

    [Serializable]
    class AdaptiveResponseObject
    {
        public string Header { get; set; }
        public string Text { get; set; }
    }
}