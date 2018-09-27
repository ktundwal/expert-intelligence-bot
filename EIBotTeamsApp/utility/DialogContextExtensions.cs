using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace Microsoft.Office.EIBot.Service.utility
{
    public static class DialogContextExtensions
    {
        public static async Task PostWithRetryAsync(this IDialogContext context, IMessageActivity activity)
        {
            var retryPolicy = BotConnectorUtility.BuildRetryPolicy();
            await retryPolicy.ExecuteAsync(async () => await context.PostAsync(activity));
        }

        public static async Task PostWithRetryAsync(this IDialogContext context, string text)
        {
            var retryPolicy = BotConnectorUtility.BuildRetryPolicy();
            await retryPolicy.ExecuteAsync(async () => await context.PostAsync(text));
        }
    }
}