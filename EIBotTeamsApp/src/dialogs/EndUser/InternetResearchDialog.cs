using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Office.EIBot.Service.dialogs.examples.moderate;
using Microsoft.Office.EIBot.Service.Properties;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    /// <summary>
    /// internet research dialog
    /// </summary>
    [Serializable]
    public class InternetResearchDialog : IDialog<bool>
    {
        private const string DescriptionKey = "description";
        private const string DeadlineKey = "deadline";
        private const string AdditionalInfoKey = "additionalInfoFromUser";
        private const string VsoIdKey = "VsoId";
        private const string EndUserConversationIdKey = "EndUserConversationId";
        static readonly string Uri = ConfigurationManager.AppSettings["VsoOrgUrl"];
        static readonly string Project = ConfigurationManager.AppSettings["VsoProject"];

        private bool isSms;
        private UserProfile userProfile;
        private int minHoursToCompleteResearch;

        /// <summary>
        /// This is start of the Dialog and Prompting for User name
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task StartAsync(IDialogContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            isSms = context.Activity.ChannelId == ActivityHelper.SmsChannelId;
            userProfile = await UserProfileHelper.GetUserProfile(context);
            minHoursToCompleteResearch = Convert.ToInt32(ConfigurationManager.AppSettings["ResearchProjectViaTeamsMinHours"]);

            //// This will Prompt for Name of the user.
            //var message = isSms ? BuildIntroMessageForSms(context) : BuildIntroMessageForTeams(context);
            //await context.PostWithRetryAsync(message);

            var minLength = isSms ? 5 : 150;
            var maxLength = 1000;
            var charLimitGuidance = $"I need a minimum of {minLength} characters and maximum of {maxLength} characters to process.";
            var promptDescription = new PromptText(
                "Okay, I'll find a human freelancer who can do the research for you. " +
                $"Tell me what kind of research you'd like the freelancer to do? {(isSms ? "" : charLimitGuidance)}",
                charLimitGuidance,
                "Wrong again. Too many attempts.",
                3, minLength, maxLength);
            context.Call(promptDescription, OnDescriptionReceivedAsync);
        }

        private async Task OnDescriptionReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException((nameof(result)) + Strings.NullException);
            }

            var descriptionFromUser = await result;

            // Store description
            context.ConversationData.SetValue(DescriptionKey, descriptionFromUser);

            // confirm back again
            context.Call(new PromptYesNo(
                    $"Did I get your research request right?\n\n{descriptionFromUser}. \n\n\n\nPlease say 'yes' or 'no'.",
                    "Sorry I didn't get that. Please say 'yes' if you want to continue.",
                    "Sorry I still don't get it if you want to continue. Please reply to start again."),
                OnDescriptionConfirmationReceivedAsync);
        }

        private async Task OnDescriptionConfirmationReceivedAsync(IDialogContext context, IAwaitable<bool> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException((nameof(result)) + Strings.NullException);
            }

            var confirmed = await result;

            if (confirmed)
            {
                // Prompt for delivery date
                var prompt = new DeadlinePrompt(minHoursToCompleteResearch, GetCurrentCultureCode());
                context.Call(prompt, OnDeadlineSelected);
            }
            else
            {
                // dont proceed
                await context.PostWithRetryAsync($"Sure. Have a nice day! Please reply back to start again");
                context.Done<object>(false);
            }
        }

        private async Task OnDeadlineSelected(IDialogContext context, IAwaitable<IEnumerable<DateTime>> deadlineResult)
        {
            if (deadlineResult == null)
            {
                throw new InvalidOperationException((nameof(deadlineResult)) + Strings.NullException);
            }

            DateTime targetDate = await ProcessUserResponseToDeadline(context, deadlineResult);

            try
            {
                // Store date
                context.ConversationData.SetValue(DeadlineKey, targetDate);

                var description = context.ConversationData.GetValue<string>(DescriptionKey);

                //IMessageActivity responseMessage = isSms
                //    ? BuildWhoWhatWhenSummaryMessageForSms(context,
                //        targetDate,
                //        description)
                //    : BuildWhoWhatWhenSummaryMessageForTeams(context,
                //        targetDate,
                //        description);

                //await context.PostWithRetryAsync(responseMessage);

                var promptAdditionalInfo = new PromptText(
                    "Okay, this is what I have so far.\n\n\n\n" +
                    $"Who: {userProfile.Alias}\n\n" +
                    $"What: {description}\n\n" +
                    $"When: {targetDate}\n\n\n\n" + 
                    "Do you have anything else to add, before I submit this task to the freelancer, " +
                    "like success criteria, or formatting requests? You can also add hyperlinks if you like. \n\n\n\n" +
                    "Please say 'none' if you don't have anything else to add. You can clarify later if needed.",
                    "Please try sending additional info again.", "Error understanding additional info. Too many attempts.", 2, 0);

                context.Call(promptAdditionalInfo, OnAdditionalInfoReceivedAsync);
            }

            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    {"dialog", "InternetResearchDialog" },
                    {"function", "OnDeadlineSelected" }
                });
                throw;
            }
        }

        private async Task<DateTime> ProcessUserResponseToDeadline(IDialogContext context, IAwaitable<IEnumerable<DateTime>> deadlineResult)
        {
            DateTime targetDate = DateTime.UtcNow.AddHours(minHoursToCompleteResearch);
            var messageForUserWhenUserDidntSpecifyExpectedDeadline = "Sorry, I had trouble understanding. " +
                                                                     $"Lets move forward with {minHoursToCompleteResearch} from now " +
                                                                     "and you can clarify this with project manager later.";

            try
            {
                // "deadlineResult" contains the date (or array of dates) returned from the prompt
                IEnumerable<DateTime> momentOrRange = await deadlineResult;
                var momentOrRangeArray = momentOrRange as DateTime[] ?? momentOrRange.ToArray();

                // check if we have any dates. If not, select 48 hours for user
                if (momentOrRangeArray.Any())
                {
                    // pick the datetime which give us most time to complete the research
                    targetDate = momentOrRangeArray.OrderByDescending(date => date.Ticks).FirstOrDefault();
                }
                else
                {
                    await context.PostWithRetryAsync(messageForUserWhenUserDidntSpecifyExpectedDeadline);
                }
            }
            catch (TooManyAttemptsException)
            {
                await context.PostWithRetryAsync(messageForUserWhenUserDidntSpecifyExpectedDeadline);
            }

            return targetDate;
        }

        private IMessageActivity BuildWhoWhatWhenSummaryMessageForSms(IDialogContext context, DateTime targetDate, string description)
        {
            var responseMessage = context.MakeMessage();
            responseMessage.Text = "Okay, this is what I have so far.\n\n\n\n" +
                                   $"Who: {userProfile}\n\n" +
                                   $"What: {description}\n\n" +
                                   $"When: {targetDate}";
            responseMessage.TextFormat = "plain";
            return responseMessage;
        }

        private IMessageActivity BuildWhoWhatWhenSummaryMessageForTeams(IDialogContext context, DateTime targetDate, string description)
        {
            AdaptiveCard responseCard = new AdaptiveCard();
            responseCard.Body.Add(new AdaptiveTextBlock
            {
                Text = "Okay, this is what I have so far.",
                Size = AdaptiveTextSize.Default,
                Wrap = true,
                Separator = true
            });
            responseCard.Body.Add(new AdaptiveFactSet
            {
                Facts = new List<AdaptiveFact>
                    {
                        new AdaptiveFact("Who", userProfile.ToString()),
                        new AdaptiveFact("What", description),
                        new AdaptiveFact("When", targetDate.ToString()),
                    }
            });

            var responseMessage = context.MakeMessage();
            responseMessage.Attachments = new List<Attachment>
                {
                    new Attachment()
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = responseCard
                    }
                };
            return responseMessage;
        }

        private async Task OnAdditionalInfoReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException((nameof(result)) + Strings.NullException);
            }

            var additionalInfoFromUser = await result;
            context.ConversationData.SetValue(AdditionalInfoKey, additionalInfoFromUser);

            var description = context.ConversationData.GetValue<string>(DescriptionKey);
            var deadline = DateTime.Parse(context.ConversationData.GetValue<string>(DeadlineKey));

            context.Call(new ConfirmResearchRequestDialog(isSms, context.Activity.From.Name,
                    description,
                    additionalInfoFromUser,
                    deadline,
                    userProfile),
                OnConfirmResearchDialog);
        }

        private async Task OnConfirmResearchDialog(IDialogContext context, IAwaitable<bool> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException((nameof(result)) + Strings.NullException);
            }

            var sendIt = await result;

            if (sendIt)
            {
                var additionalInfoFromUser = context.ConversationData.GetValue<string>(AdditionalInfoKey);
                var description = context.ConversationData.GetValue<string>(DescriptionKey);
                var deadline = DateTime.Parse(context.ConversationData.GetValue<string>(DeadlineKey));

                var vsoTicketNumber = await VsoHelper.CreateTaskInVso(VsoHelper.ResearchTaskType,
                    context.Activity.From.Name,
                    description + Environment.NewLine + additionalInfoFromUser,
                    ConfigurationManager.AppSettings["AgentToAssignVsoTasksTo"],
                    deadline,
                    "",
                    userProfile,
                    context.Activity.ChannelId);

                context.ConversationData.SetValue(VsoIdKey, vsoTicketNumber);
                context.ConversationData.SetValue(EndUserConversationIdKey, context.Activity.Conversation.Id);

                try
                {
                    var conversationTitle = $"Web research request from {userProfile} via {context.Activity.ChannelId} due {deadline}";
                    string agentConversationId = await ConversationHelpers.CreateAgentConversationEx(context,
                        conversationTitle,
                        CreateCardForAgent(context,
                            additionalInfoFromUser,
                            description,
                            deadline,
                            vsoTicketNumber),
                        userProfile);

                    EndUserAndAgentConversationMappingState state =
                        new EndUserAndAgentConversationMappingState(vsoTicketNumber.ToString(),
                            context.Activity.From.Name,
                            context.Activity.From.Id,
                            context.Activity.Conversation.Id,
                            agentConversationId);

                    await state.SaveInVso(vsoTicketNumber.ToString());

                    await context.PostWithRetryAsync("Sure. I have sent your request to a freelancer. " +
                                                     $"Please use #{vsoTicketNumber} for referencing this request in future. " +
                                                     "At this point, any message you send will be sent directly to the freelancer. They may take time to respond, " +
                                                     "or may have clarifying questions which I will relay back to you.");

                    context.Done<object>(true);
                }
                catch (System.Exception e)
                {
                    await context.PostWithRetryAsync("Sorry, I ran into an issue while connecting with agent. Please try again later.");
                    WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string> { { "function", "OnConfirmResearchDialog.CreateAgentConversation" } });
                    context.Done<object>(false);
                }
            }
            else
            {
                await context.PostWithRetryAsync("Okay, I have cancelled this request.");
                context.Done<object>(false);
            }
        }

        private AdaptiveCard CreateCardForAgent(IDialogContext context, string additionalInfoFromUser, string description, DateTime deadline, int vsoTicketNumber)
        {
            AdaptiveCard card = new AdaptiveCard();
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = $"Web research request from {userProfile} via {context.Activity.ChannelId} due {deadline}",
                Size = AdaptiveTextSize.Large,
                Wrap = true,
                Separator = true
            });
            card.Body.Add(new AdaptiveFactSet
            {
                Facts = new List<AdaptiveFact>
                            {
                                new AdaptiveFact("Who", context.Activity.From.Name),
                                new AdaptiveFact("What", description),
                                new AdaptiveFact("Additional Info", additionalInfoFromUser),
                                new AdaptiveFact("When", deadline.ToString()),
                            }
            });
            card.Actions.Add(new AdaptiveOpenUrlAction()
            {
                Url = new Uri($"{Uri}/{Project}/_workitems/edit/{vsoTicketNumber}"),
                Title = $"Vso: {vsoTicketNumber}"
            });
            card.Body.Add(new AdaptiveTextBlock { Text = "Tips", Wrap = true });
            card.Body.Add(new AdaptiveTextBlock { Text = "- Please use **reply to user** command to send message to user.", Wrap = true });
            card.Body.Add(new AdaptiveTextBlock { Text = "- Please post jobs to UpWork manually. Support to posting via bot is coming soon.", Wrap = true });
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = "- Sending attachments is not supported. " +
                                                        "Please send research documents as a **link**. " +
                                                        "Upload file in 'files' tab, use it to go to SharePoint site. From there 'Share > Email'. " +
                                                        "User alias is in VSO ticket",
                Wrap = true
            });
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = "- When research is complete, please seek acknowledgement from end user. " +
                                                        "Once done, please **close VSO ticket**, else user wont be able to create new one",
                Wrap = true
            });
            return card;
        }

        private static string GetCurrentCultureCode()
        {
            // Use English as default culture since the this sample bot that does not include any localization resources
            // Thread.CurrentThread.CurrentUICulture.IetfLanguageTag.ToLower() can be used to obtain the user's preferred culture
            return "en-us";
        }

        private static IMessageActivity BuildIntroMessageForSms(IDialogContext context)
        {
            var message = context.MakeMessage();

            var heroCard = new HeroCard
            {
                Title = "Internet research request",
                Subtitle = "I can find a freelancer who will pull together some research for you",
                Text = "Here are few examples of completed research",
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.OpenUrl,
                        "Top strategies to stand-out on Linked In",
                        value:
                        "https://microsoft.sharepoint.com/:w:/t/OfficeandtheGigEconomy/EaupzMfIrlRMiJ821DGbkqIBTMSkqlUduR85E6boQRK43w?e=opeRjW"),
                    new CardAction(ActionTypes.OpenUrl,
                        "Average price for a Raspberry Pi 3 in Washington",
                        value:
                        "https://microsoft.sharepoint.com/:w:/t/OfficeandtheGigEconomy/EcrZDqoBqzxDrRsdQoKsNNYBrspa7e7uZYNoosFqxNJyrA?e=2a6BX6"),
                }
            };

            message.Attachments.Add(heroCard.ToAttachment());

            return message;
        }
    }
}