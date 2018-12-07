using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Office.EIBot.Service.Properties;
using Microsoft.Office.EIBot.Service.utility;
using Newtonsoft.Json.Linq;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    [Serializable]
    public class PresentationDialog : IDialog<object>
    {
        public static string PurposeValue = "PurposeValue";
        public static string StyleValue = "StyleValue";
        public static string ThemeValue = "ThemeValue";
        public static string ExtraInfo = "ExtraInfo";

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

            if(activity.Text == PresentationDialogStrings.NewProject.ToLower() 
                || activity.Text == PresentationDialogStrings.ProgressReport.ToLower()
                || activity.Text == PresentationDialogStrings.Educate.ToLower())
            {
                context.UserData.SetValue(PurposeValue, activity.Text);

                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationStyleCard(activity.Text)));
                context.Wait(this.MessageReceivedAsyncFromStyleSelection);

            } else if (activity.Text == PresentationDialogStrings.OtherOption)
            {
                context.UserData.SetValue(PurposeValue, activity.Text);
                // Go to end with summary
            } else
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationPurposeOptions()));
                context.Wait(this.MessageReceivedAsyncToStyleSelection);
            }
        }

        private async Task MessageReceivedAsyncFromStyleSelection(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var activity = await result;

            if (activity.Text == "modern"
                || activity.Text == "corporate"
                || activity.Text == "abstract")
            {
                context.UserData.SetValue(StyleValue, activity.Text);
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationColorVariationCard(activity.Text)));
                context.Wait(this.SelectColorVariation);
            }
            else if (activity.Text == "pick for me")
            {
                // Show more options or go to end! 
            } else
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationStyleCard(activity.Text)));
                context.Wait(this.MessageReceivedAsyncFromStyleSelection);
            }
        }

        private async Task SelectColorVariation(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var activity = await result;

            if (activity.Text == "light" || activity.Text == "dark" || activity.Text == "colorful")
            {
                context.UserData.SetValue(ThemeValue, activity.Text);
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.AnythingElseCard()));
                context.Wait(this.ExtraInformation);

            } else
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationStyleCard(activity.Text)));
                context.Wait(this.MessageReceivedAsyncFromStyleSelection);
            }
        }

        private async Task ExtraInformation(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var activity = await result;
            var comment = ((JObject)activity.Value).Value<string>("comment");

            if(comment.Length > 0)
            {
                context.UserData.SetValue(ExtraInfo, comment);
            }
            await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.ConfirmationCard()));
            context.Wait(this.GoToSummaryCard);
        }

        private async Task GoToSummaryCard(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var activity = await result;

            await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationSummaryCard(context)));
            context.Wait(this.GoToSummaryCard);
        }

        private static void ThrowExceptionIfResultIsNull(IAwaitable<object> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException(nameof(result) + Strings.NullException);
            }
        }
    }
}