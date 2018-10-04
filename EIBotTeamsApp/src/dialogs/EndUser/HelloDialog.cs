using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Office.EIBot.Service.Properties;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    /// <summary>
    /// This is Begin Dialog Class. Main purpose of this class is to notify users that Child dialog has been called 
    /// and its a Basic example to call Child dialog from Root Dialog.
    /// </summary>

    [Serializable]
    public class HelloDialog : IDialog<object>
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
                /*
                 *  Check if we know about user.
                 * If this is over SMS we need alias
                 * If this is over Teams we need mobile phone and alias.
                 */
                utility.UserProfile userProfile = await UserProfile.GetUserProfileFromStoreOrAskFromUser(context);
                context.UserData.SetValue("userProfile", userProfile);
                context.Call(new InternetResearchDialog(), EndDialog);

                // This code is commented out because at the moment we are going
                // straight to internet research requests
                // await EngageBot(context);
            }
        }

        private async Task EngageBot(IDialogContext context)
        {
            var message = context.MakeMessage();
            Attachment attachment = BuildOptionsForNewUserWithResearchPptApptOptions(context);
            message.Attachments.Add(attachment);
            await context.PostWithRetryAsync(message);
            context.Wait(MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException((nameof(result)) + Strings.NullException);
            }

            //Prompt the user with welcome message before game starts
            IMessageActivity message = await result;

            WebApiConfig.TelemetryClient.TrackEvent("Message received", new Dictionary<string, string>()
            {
                {"class", "HelloDialog" },
                {"from", context.Activity.From.Name },
                {"message", message.Text },
            });

            if (message.Text.ToLower().StartsWith("closeproject"))
            {
                await CloseProject(context, message);
            }
            else if (message.Text.ToLower().StartsWith("getproject"))
            {
                await GetProject(context, message);
            }
            else if (message.Text.ToLower().StartsWith("research"))
            {
                context.Call(new InternetResearchDialog(), EndDialog);
            }
            else if (message.Text.ToLower().StartsWith("ppt"))
            {
                await context.PostWithRetryAsync("'PowerPoint improvement' functionality is still under development.");
                context.Done<object>(null);
            }
            else if (message.Text.ToLower().StartsWith("appointment"))
            {
                await context.PostWithRetryAsync("'virtual assistant' functionality is still under development.");
                context.Done<object>(null);
            }
            else if (message.Text.ToLower().StartsWith("agent"))
            {
                context.Call(new TalkToAnAgentDialog(), EndDialog);
            }
            else
            {
                context.Call(new UserHelpDialog(), EndDialog);
            }
        }

        private async Task GetProject(IDialogContext context, IMessageActivity message)
        {
            if (TryParseVsoId(message.Text, out int vsoId))
            {
                await context.PostWithRetryAsync($"Let me get the status of {vsoId}");
                try
                {
                    string projectDetails = await VsoHelper.GetProjectSummary(vsoId);
                    await context.PostWithRetryAsync(projectDetails);
                    await PromptForConnectToAgentAfterGettingProjectDetails(context);
                }
                catch (System.Exception e)
                {
                    Trace.TraceInformation($"Sorry, I ran into an error closing project #{vsoId}. Exception = {e.Message}");
                    context.Call(new UserHelpDialog(), EndDialog);
                }
            }
            else
            {
                await LetUserKnowWeRanIntoAnIssueAndSendToAgentDialog(context);
            }
        }

        private async Task CloseProject(IDialogContext context, IMessageActivity message)
        {
            if (TryParseVsoId(message.Text, out int vsoId))
            {
                await context.PostWithRetryAsync($"Sure I can help close project #{vsoId}");
                await VsoHelper.CloseProject(vsoId);
                await context.PostWithRetryAsync($"{vsoId} project is now closed.");
                await PromptForCreatingNewProjectAfterClosingExistingOne(context);
            }
            else
            {
                await context.PostWithRetryAsync("Sorry, I ran into an error");
                context.Call(new UserHelpDialog(), EndDialog);
            }
        }

        private async Task LetUserKnowWeRanIntoAnIssueAndSendToAgentDialog(IDialogContext context)
        {
            await context.PostWithRetryAsync("Sorry, I ran into an error");
            context.Call(new UserHelpDialog(), EndDialog);
        }

        private async Task PromptForConnectToAgentAfterGettingProjectDetails(IDialogContext context)
        {
            try
            {
                var message = context.MakeMessage();
                message.Attachments.Add(BuildYesNoHeroCard("Do you want to talk to an agent about this project?"));
                await context.PostWithRetryAsync(message);
                context.Wait(TalkToAnAgentResponse);
            }
            catch (System.Exception e)
            {
                Trace.TraceInformation($"Error prompting to connect with an agent after getting details of a project. Exception = {e.Message}");
                context.Call(new UserHelpDialog(), EndDialog);
            }
        }

        private async Task PromptForCreatingNewProjectAfterClosingExistingOne(IDialogContext context)
        {
            try
            {
                var message = context.MakeMessage();
                message.Attachments.Add(BuildYesNoHeroCard("Do you want to create new project?"));
                await context.PostWithRetryAsync(message);
                context.Wait(StartNewProjectResponse);
            }
            catch (System.Exception e)
            {
                Trace.TraceInformation($"Error prompting to create new project after closing existing one. Exception = {e.Message}");
                context.Call(new UserHelpDialog(), EndDialog);
            }
        }

        private async Task TalkToAnAgentResponse(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException((nameof(result)) + Strings.NullException);
            }
            IMessageActivity message = await result;
            if (message.Text == "yes")
            {
                context.Call(new TalkToAnAgentDialog(), EndDialog);
            }
            else if (message.Text == "no")
            {
                await context.PostWithRetryAsync("Ok. Have a nice day!");
                context.Done<object>(null);
            }
            else
            {
                context.Call(new UserHelpDialog(), EndDialog);
            }
        }

        private async Task StartNewProjectResponse(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException((nameof(result)) + Strings.NullException);
            }
            IMessageActivity message = await result;
            if (message.Text == "yes")
            {
                var newMessage = context.MakeMessage();
                Attachment attachment = BuildOptionsForNewUserWithResearchPptApptOptions(context);
                newMessage.Attachments.Add(attachment);
                await context.PostWithRetryAsync(newMessage);
                context.Wait(MessageReceivedAsync);
            }
            else if (message.Text == "no")
            {
                await context.PostWithRetryAsync("Ok. Have a nice day!");
                context.Done<object>(null);
            }
            else
            {
                context.Call(new UserHelpDialog(), EndDialog);
            }
        }

        private bool TryParseVsoId(string message, out int i)
        {
            try
            {
                i = Convert.ToInt32(message.Split(' ')[1]);
                return true;
            }
            catch (System.Exception e)
            {
                Trace.TraceInformation($"Error parsing vsoId from {message}. Exception = {e.Message}");
                i = 0;
                return false;
            }
        }

        private Task EndDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done<object>(null);
            return Task.CompletedTask;
        }

        private Task EndDialog(IDialogContext context, IAwaitable<bool> result)
        {
            context.Done<object>(null);
            return Task.CompletedTask;
        }

        private static Attachment BuildYesNoHeroCard(string question)
        {
            return new HeroCard
            {
                Text = question,
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, "Yes", value: "yes"),
                    new CardAction(ActionTypes.ImBack, "No", value: "no"),
                }
            }.ToAttachment();
        }

        private static Attachment BuildOptionsForNewUserWithResearchPptApptOptions(IDialogContext context)
        {
            var heroCard = new HeroCard
            {
                Title = $"Hello {UserProfile.GetFriendlyName(context, false)}! I am Expert Intelligence Bot.",
                Subtitle = "I am supported by experts who can work for you.",
                Text = "We can do a few things. Please select one of the options so I can collect few information to get started. " +
                       "After that a project manager will review your request and follow up." +
                       $"You can also reach out to me by sending SMS at {ConfigurationManager.AppSettings["BotPhoneNumber"]}",
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, "Internet Research", value: "research"),
                    new CardAction(ActionTypes.ImBack, "PowerPoint Improvements", value: "ppt"),
                    new CardAction(ActionTypes.ImBack, "virtual assistant", value: "virtual assistant"),
                    new CardAction(ActionTypes.ImBack, "Talk to an agent", value: "agent")
                }
            };

            return heroCard.ToAttachment();
        }

        private static Attachment BuildOptionsForExistingProject(TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem workItem)
        {
            var heroCard = new HeroCard
            {
                Title = "Hello! I am Expert Intelligence Bot.",
                Subtitle = "I am supported by experts who can work for you." +
                           $"You can also reach out to me by sending SMS at {ConfigurationManager.AppSettings["BotPhoneNumber"]}",
                Text = $"I am tracking project #{workItem.Id} " +
                       $"due on {workItem.Fields["Microsoft.VSTS.Scheduling.TargetDate"]} " +
                       $"Description: {workItem.Fields["System.Description"]}). " +
                       "Before we begin working on new one, we need to first close existing project one",
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, $"Get details of project #{workItem.Id}", value: $"getproject {workItem.Id}"),
                    new CardAction(ActionTypes.ImBack, $"Close project #{workItem.Id}", value: $"closeproject {workItem.Id}")
                }
            };

            return heroCard.ToAttachment();
        }
    }
}