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
        public async Task StartAsync(IDialogContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.UserData.SetValue(UserData.Style, "");
            
            await context.PostAsync(
                    ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationIntro()));
            
            context.Wait(ToShowExamplesOrLetsBegin);
        }

        private async Task ToShowExamplesOrLetsBegin(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var activity = await result;

            if (activity.Text == PresentationDialogStrings.LetsBegin.ToLower())
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.V2PresentationPurpose()));
                context.Wait(ToPurposeSelection);
            } else if (activity.Text == PresentationDialogStrings.V2ShowExamples.ToLower())
            {
                await context.PostAsync(CardBuilder.V2ShowExamples(context));
                context.Wait(ToLastStepOrWorkflow);
            }
            else
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context,
                   CardBuilder.PresentationIntro()));
                context.Wait(ToShowExamplesOrLetsBegin);
            }
        }

        private async Task ToLastStepOrWorkflow(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);
            var activity = await result;

            if (activity.Text == PresentationDialogStrings.LetsBegin.ToLower())
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.V2PresentationPurpose()));
                context.Wait(ToPurposeSelection);
            } else
            {
                context.UserData.SetValue(UserData.Style, $"Make mine like this: {activity.Text}");
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.AnythingElseCard()));
                context.Wait(ExtraInformation);
            }
        }

        private async Task ToPurposeSelection(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var activity = await result;

            if(activity.Text == PresentationDialogStrings.NewProject.ToLower() 
                || activity.Text == PresentationDialogStrings.ProgressReport.ToLower()
                || activity.Text == PresentationDialogStrings.Educate.ToLower())
            {
                context.UserData.SetValue(UserData.Purpose, activity.Text);
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationColorVariationCard(activity.Text)));
                context.Wait(ToColorSelection);

            } else if (activity.Text == PresentationDialogStrings.OtherOption)
            {
                context.UserData.SetValue(UserData.Purpose, activity.Text);
                // Go to end with summary
            } else
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.V2PresentationPurpose()));
                context.Wait(ToPurposeSelection);
            }
        }

        private async Task ToColorSelection(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var activity = await result;

            if (activity.Text == "light" || activity.Text == "dark" || activity.Text == "colorful")
            {
                context.UserData.SetValue(UserData.Style, $"{context.UserData.GetValue<string>(UserData.Style)}{activity.Text}");
                context.UserData.SetValue(UserData.Color, activity.Text);

                await context.PostAsync(
                    ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.V2IllustrationsCard()));
                context.Wait(ToIllustrationSelection);
            } else
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationColorVariationCard(activity.Text)));
                context.Wait(ToColorSelection);
            }
        }

        private async Task ToIllustrationSelection(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);
            var activity = await result;

            if (activity.Text == "photos" || activity.Text == "illustrations" || activity.Text == "shapes")
            {
                context.UserData.SetValue(UserData.Style, $"{context.UserData.GetValue<string>(UserData.Style)}, {activity.Text}");
                context.UserData.SetValue(UserData.Visuals, activity.Text);
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.AnythingElseCard()));
                context.Wait(ExtraInformation);
            }
            else
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.V2IllustrationsCard()));
                context.Wait(ToIllustrationSelection);
            }
        }

        private async Task ExtraInformation(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var activity = await result;
            var comment = activity.Text;
            //var comment = ((JObject)activity.Value).Value<string>("comment");

            if(comment.Length > 0)
            {
                context.UserData.SetValue(UserData.Extra, comment);
            }
            await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.ConfirmationCard()));
            context.Wait(this.GoToSummaryCard);
        }

        private async Task GoToSummaryCard(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);
            await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.PresentationSummaryCard(context)));
            context.Wait(ToSendJob);
        }

        private async Task ToSendJob(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);
            var activity = await result;

            if (activity.Text == "looks good")
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context,
                    CardBuilder.V2VsoTicketCard(123, "https://www.microsoft.com")));
                context.Wait(ToResultPage);
            } else if (activity.Text == "i want to change something")
            {
                // Show the change card...
                context.Reset();
            }
        }
        private async Task ToResultPage(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);
            var activity = await result;
            await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context,
                CardBuilder.V2PresentationResponse("John Doe")));
            context.Wait(ToResultConfirmation);
        }

        private async Task ToResultConfirmation(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);
            var activity = await result;

            if (activity.Text == "this is complete")
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context,
                    CardBuilder.V2Ratings()));
                context.Wait(ToRatingsResult);
            }
            else if (activity.Text == "i want a free revision")
            {
                // Show the change card...
                context.Reset();
            }
        }

        private async Task ToRatingsResult(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);
            var activity = await result;

            if (Int32.TryParse(activity.Text, out int rating))
            {
                context.UserData.SetValue(UserData.Rating, rating);
                if (rating <= 3)
                {
                    await context.PostAsync(
                        ActivityHelper.CreateResponseMessageWithAdaptiveCard(context,
                            CardBuilder.V2Feedback(false, true)));
                    context.Wait(ToFeedback);
                }
                else
                {
                    await PostLearningContent(context);
                    await context.PostAsync(
                        ActivityHelper.CreateResponseMessageWithAdaptiveCard(context,
                            CardBuilder.V2Feedback(false, false)));
                    context.Wait(ToFeedback);
                }
            }
        }

        private async Task ToFeedback(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);
            var activity = await result;

            if (context.UserData.GetValue<int>(UserData.Rating) <= 3)
            {
                await PostLearningContent(context);
            }
            context.UserData.SetValue(UserData.Feedback, activity.Text);
            context.Reset();
        }

        private static async Task PostLearningContent (IDialogContext context)
        {
            await context
                .PostAsync(
                    ActivityHelper
                        .CreateResponseMessageWithAdaptiveCard(
                            context,
                            CardBuilder.V2Learning(
                                "Great. Will you be presenting this during a meeting? If so, we recommend checking out this LinkedIn Learning course on how to deliver and effective presentation:",
                                "https://www.linkedin.com/",
                                null,
                                "PowerPoint Tips and Tricks for Business Presentations"
                            )
                        )
                    );
        }

        private static void ThrowExceptionIfResultIsNull(IAwaitable<object> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException(nameof(result) + Strings.NullException);
            }
        }
    }

    public static class UserData
    {
        public const string Purpose = "purpose";
        public const string Style = "style";
        public const string Color = "color";
        public const string Visuals = "visuals";
        public const string Extra = "extra";
        public const string Rating = "rating";
        public const string Feedback = "feedback";
    }
}