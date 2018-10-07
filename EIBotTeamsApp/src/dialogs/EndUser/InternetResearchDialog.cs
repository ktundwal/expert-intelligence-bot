using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Office.EIBot.Service.Properties;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;
using Microsoft.Teams.TemplateBotCSharp.Dialogs;

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

            isSms = ActivityHelper.IsPhoneNumber(context.Activity.From.Name);

            // This will Prompt for Name of the user.
            var message = context.MakeMessage();
            var attachment = GetIntroHeroCard();

            message.Attachments.Add(attachment);

            await context.PostWithRetryAsync(message);

            var minLength = isSms ? 10 : 150;
            var maxLength = 1000;
            var charLimitGuidance = $"I need a minimum of {minLength} characters and maximum of {maxLength} characters to process";
            var promptDescription = new PromptText(
                "Okay, I'll find a human freelancer who can do the research for you. Tell me what kind of research you'd like the freelancer to do?. " +
                charLimitGuidance,
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

            await context.PostWithRetryAsync($"Got it. When do you need this research to be completed? We'll need at least 48 hrs");

            // Prompt for delivery date
            var prompt = new DeadlinePrompt(Convert.ToInt32(ConfigurationManager.AppSettings["ResearchProjectViaTeamsMinHours"]), GetCurrentCultureCode());
            context.Call(prompt, OnDeadlineSelected);
        }

        private async Task OnDeadlineSelected(IDialogContext context, IAwaitable<IEnumerable<DateTime>> result)
        {
            try
            {
                // "result" contains the date (or array of dates) returned from the prompt
                IEnumerable<DateTime> momentOrRange = await result;

                // pick the datetime which give us most time to complete the research
                var targetDate = momentOrRange.OrderByDescending(date => date.Ticks).First();

                // Store date
                context.ConversationData.SetValue(DeadlineKey, targetDate);

                var description = context.ConversationData.GetValue<string>(DescriptionKey);


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
                        new AdaptiveFact("Who", context.Activity.From.Name),
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

                await context.PostWithRetryAsync(responseMessage);

                var promptAdditionalInfo = new PromptText(
                    "Do you have anything else to add, before I submit this task to the freelancer? " +
                    "Like success criteria, or formatting requests. You can also add hyperlinks if you like. " +
                    "Please say 'none' if you don't have anything else to add. You can clarify later if needed.",
                    "Please try again", "Wrong again. Too many attempts.", 2, 0);

                context.Call(promptAdditionalInfo, OnAdditionalInfoReceivedAsync);
            }
            catch (TooManyAttemptsException)
            {
                await context.PostWithRetryAsync("TooManyAttemptsException. Restarting now...");
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

            context.Call(new ConfirmResearchRequestDialog(context.Activity.From.Name,
                    description,
                    additionalInfoFromUser,
                    deadline),
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
                    "");

                context.ConversationData.SetValue(VsoIdKey, vsoTicketNumber);
                context.ConversationData.SetValue(EndUserConversationIdKey, context.Activity.Conversation.Id);

                try
                {
                    string agentConversationId = await CreateAgentConversation(context,
                        additionalInfoFromUser,
                        description,
                        deadline,
                        vsoTicketNumber);

                    EndUserAndAgentConversationMappingState state =
                        new EndUserAndAgentConversationMappingState(vsoTicketNumber.ToString(),
                            context.Activity.From.Name,
                            context.Activity.From.Id,
                            context.Activity.Conversation.Id,
                            agentConversationId);

                    await state.SaveInVso(vsoTicketNumber.ToString());

                    await context.PostWithRetryAsync("Sure. I have sent your request to project manager. " +
                                                     $"Please use #{vsoTicketNumber} for referencing this request in future. " +
                                                     "At this point, any message you send will be sent directly to project manager. They may take time to respond. " +
                                                     "They may have clarifying questions which I will relay back to you.");

                    context.Done<object>(true);
                }
                catch (System.Exception e)
                {
                    await context.PostWithRetryAsync("Sorry, I ran into an issue while connecting with agent. Please try again later.");
                    WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string> {{"function", "OnConfirmResearchDialog.CreateAgentConversation" } });
                    context.Done<object>(false);
                }
            }
            else
            {
                await context.PostWithRetryAsync("Okay, I have cancelled this request.");
                context.Done<object>(false);
            }
        }

        private static async Task<string> CreateAgentConversation(IDialogContext context,
            string additionalInfoFromUser,
            string description,
            DateTime deadline,
            int vsoTicketNumber)
        {
            using (var connectorClient = await BotConnectorUtility.BuildConnectorClientAsync(context.Activity.ServiceUrl))
            {
                var conversationResourceResponse = await ConversationHelpers.CreateAgentConversation(
                    await GetAgentChannelId(),
                    CreateCardForAgent(context, additionalInfoFromUser, description, deadline, vsoTicketNumber),
                    $"New research request from {context.Activity.From.Name}",
                    connectorClient,
                    vsoTicketNumber,
                    context.Activity as IMessageActivity);

                return conversationResourceResponse.Id;
            }
        }

        private static AdaptiveCard CreateCardForAgent(IDialogContext context, string additionalInfoFromUser, string description, DateTime deadline, int vsoTicketNumber)
        {
            AdaptiveCard card = new AdaptiveCard();
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = $"New research request from {context.Activity.From.Name}. VSO:{vsoTicketNumber}",
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
            return card;
        }

        // todo: katundwa remove hardcoded channel id
        private static ChannelInfo GetHardcodedChannelId()
        {
            return new ChannelInfo("19:c20b196747424d8db51f6c00a8a9efa8@thread.skype", "Research Agents");
        }

        private static async Task<ChannelInfo> GetAgentChannelId()
        {
            return await IdTable.GetAgentChannelInfo();
        }

        private static string GetCurrentCultureCode()
        {
            // Use English as default culture since the this sample bot that does not include any localization resources
            // Thread.CurrentThread.CurrentUICulture.IetfLanguageTag.ToLower() can be used to obtain the user's preferred culture
            return "en-us";
        }

        private static Attachment GetIntroHeroCard()
        {
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

            return heroCard.ToAttachment();
        }
    }
}