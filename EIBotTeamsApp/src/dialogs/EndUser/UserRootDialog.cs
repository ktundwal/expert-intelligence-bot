using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Scorables;
using Microsoft.Bot.Connector;
using Microsoft.Office.EIBot.Service.Properties;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    /// <summary>
    /// This is Root Dialog for user
    /// </summary>

    [Serializable]
    public class UserRootDialog : DispatchDialog
    {
        private const string ProjectTypeKey = "projectType";
        private const string ProjectTypeResearch = "Web research";
        private const string ProjectTypePresentation = "Presentation design";
        private const string ProjectTypeTasks = "Personal tasks";

        #region Internet research Pattern

        //[RegexPattern(DialogMatches.PerformInternetResearchMatch)]
        //[ScorableGroup(1)]
        //public void PerformInternetResearch(IDialogContext context, IActivity activity)
        //{
        //    context.Call(new InternetResearchDialog(), EndInternetResearchDialog);
        //}

        //public async Task EndInternetResearchDialog(IDialogContext context, IAwaitable<bool> awaitable)
        //{
        //    await context.PostWithRetryAsync("Have a nice day!");
        //    context.Done<object>(null);
        //}

        #endregion

        #region Hello Dialog

        [MethodBind]
        [ScorableGroup(1)]
        public async Task RunHelloDialog(IDialogContext context, IActivity activity)
        {
            //context.Call(new HelloDialog(), EndHelloDialog);
            //context.Call(new ExpertConnectDialog(), EndHelloDialog);
            //await AskUserTypeOfProject(context);

            if (context.Activity.ChannelId == ActivityHelper.SmsChannelId)
            {
                context.Call(new ExpertConnectDialog(), EndHelloDialog);
            } else if (context.Activity.ChannelId == ActivityHelper.MsTeamChannelId)
            {
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.IntroductionCard()));
                context.Wait(this.IntroductionCardResponseMessageReceivedAsync);
            }
        }

        private async Task IntroductionCardResponseMessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var activity = await result;

            if (activity.Text.ToLower() == PresentationDialogStrings.PresentationDesign.ToLower())
            {
                context.Call(new PresentationDialog(), EndHelloDialog);
            } else if (activity.Text.ToLower() == PresentationDialogStrings.WebResearch.ToLower())
            {
                context.Call(new ExpertConnectDialog(), EndHelloDialog);
            }
            else // if (activity.Text.ToLower() == PresentationDialogStrings.PersonalTasks.ToLower())
            {
                await context.PostAsync("Please select Web research OR Presentation");
                await context.PostAsync(ActivityHelper.CreateResponseMessageWithAdaptiveCard(context, CardBuilder.IntroductionCard()));
                context.Wait(this.IntroductionCardResponseMessageReceivedAsync);
            }
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

        #region Project Types

        private async Task AskUserTypeOfProject(IDialogContext context)
        {
            var message = context.MakeMessage();
            Attachment attachment = BuildOptionsForNewUserWithResearchPptTasksOptions(context);
            message.Attachments.Add(attachment);
            await context.PostWithRetryAsync(message);
            context.Wait(OnProjectTypeReceivedAsync);
        }

        private async Task OnProjectTypeReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            ThrowExceptionIfResultIsNull(result);

            IMessageActivity resultActivity = await result;

            string projectType = resultActivity.Text.ToLower();

            WebApiConfig.TelemetryClient.TrackEvent("OnProjectTypeReceivedAsync", new Dictionary<string, string>()
            {
                {"class", "UserRootDialog" },
                {"from", context.Activity.From.Name },
                {ProjectTypeKey, projectType },
            });

            context.ConversationData.SetValue(ProjectTypeKey, projectType);

            switch (projectType)
            {
                case ProjectTypeResearch:
                    context.Call(new ExpertConnectDialog(), EndHelloDialog);
                    break;
                case ProjectTypePresentation:
                    context.Call(new PresentationDesignDialog(), EndHelloDialog);
                    break;
                case ProjectTypeTasks:
                default:
                    await context.PostWithRetryAsync($"Sorry, I don't support {projectType}. Please retry.");
                    context.Wait(OnProjectTypeReceivedAsync);
                    break;
            }
        }

        private static Attachment BuildOptionsForNewUserWithResearchPptTasksOptions(IDialogContext context)
        {
            if (context.Activity.ChannelId == ActivityHelper.MsTeamChannelId &&
                context.Activity.Conversation.ConversationType == ActivityHelper.ChatTypeIsPersonal)
            {
                var friendlyName = context.Activity.From.Name;

                var heroCard = new HeroCard
                {
                    Title = $"Hello {friendlyName}! I am Expert Connect Bot.",
                    Subtitle = "I am supported by experts from UpWork and FancyHands, who can work for you.",
                    Text = "Please select an option below. I'll collect some information to get started, " +
                           "then a project manager will review your request and follow up.",
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack,
                            "Web research",
                            "https://image.freepik.com/free-icon/keyword-research_318-50732.jpg",
                            value: ProjectTypeResearch),
                        new CardAction(ActionTypes.ImBack,
                            "Presentation design",
                            "https://image.flaticon.com/icons/svg/29/29125.svg",
                            value: ProjectTypePresentation),
                    }
                };

                return heroCard.ToAttachment();
               
            }
            throw new System.Exception("PPT options is only available via MSTeams and 1:1 chat");

        }

        private static void ThrowExceptionIfResultIsNull(IAwaitable<object> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException(nameof(result) + Strings.NullException);
            }
        }

        #endregion
    }
}