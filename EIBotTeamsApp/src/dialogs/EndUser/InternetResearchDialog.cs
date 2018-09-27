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
    /// This is Game Dialog Class. Here are the steps to play the games -
    ///  1. Its gives 3 options to users to choose.
    ///  2. If user choose any of the option, Bot take confirmation from the user about the choice.
    ///  3. Bot reply to the user based on user choice.
    /// </summary>
    [Serializable]
    public class InternetResearchDialog : IDialog<bool>
    {
        static readonly string Uri = ConfigurationManager.AppSettings["VsoOrgUrl"];
        static readonly string Project = ConfigurationManager.AppSettings["VsoProject"];

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
            //Set the Last Dialog in Conversation Data
            context.UserData.SetValue(Strings.LastDialogKey, Strings.LastDialogGameDialog);

            // This will Prompt for Name of the user.
            var message = context.MakeMessage();
            var attachment = GetIntroHeroCard();

            message.Attachments.Add(attachment);

            await context.PostWithRetryAsync(message);

            var promptDescription = new PromptText(
                "Please describe your research request (Minimum 200 and maximum 500 characters)",
                "I need a minimum of 200 characters and maximum of 500 characters to process",
                "Wrong again. Too many attempts.",
                3);
            context.Call<string>(promptDescription, OnDescriptionReceivedAsync);
        }

        private async Task OnDescriptionReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException((nameof(result)) + Strings.NullException);
            }

            var descriptionFromUser = await result;

            await context.PostWithRetryAsync($"I can help with {descriptionFromUser}.");

            // Store description
            context.ConversationData.SetValue("description", descriptionFromUser);

            await context.PostWithRetryAsync($"When do you need this by?");

            // Prompt for delivery date
            var prompt = new DeadlinePrompt(GetCurrentCultureCode());
            context.Call(prompt, OnDeadlineSelected);
        }

        private async Task OnDeadlineSelected(IDialogContext context, IAwaitable<IEnumerable<DateTime>> result)
        {
            try
            {
                // "result" contains the date (or array of dates) returned from the prompt
                IEnumerable<DateTime> momentOrRange = await result;
                //var deadline = DeadlinePrompt.MomentOrRangeToString(momentOrRange);

                var targetDate = momentOrRange.First();

                // Store date
                context.ConversationData.SetValue("deadline", targetDate);

                var description = context.ConversationData.GetValue<string>("description");

                var messageWithDescriptionAndDeadline = $"Who {context.Activity.From.Name}\n\n"
                                                        + $"What {description}\n\n"
                                                        + $"When {targetDate}\n\n";

                await context.PostWithRetryAsync($"Ok. This is what I have so far\n\n{messageWithDescriptionAndDeadline}");

                var promptAdditionalInfo = new PromptText(
                    "Do you have anything else to add? Information like success criteria and formatting requirements would be helpful. " +
                    "Please say 'none' if you don't have anything else to add. My experts will clarify later.",
                    "Please try again", "Wrong again. Too many attempts.", 2, 0, 500);

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

            var description = context.ConversationData.GetValue<string>("description");
            var deadline = DateTime.Parse(context.ConversationData.GetValue<string>("deadline"));

            var vsoTicketNumber = await VsoHelper.CreateTaskInVso(VsoHelper.ResearchTaskType,
                    context.Activity.From.Name,
                    description + Environment.NewLine + additionalInfoFromUser,
                    "mamottol@microsoft.com",
                    deadline,
                    "");

            var summary = new AdaptiveFactSet
            {
                Facts = new List<AdaptiveFact>
                {
                    new AdaptiveFact("Who", context.Activity.From.Name),
                    new AdaptiveFact("What", description),
                    new AdaptiveFact("Additional Info", additionalInfoFromUser),
                    new AdaptiveFact("When", deadline.ToString()),
                }
            };

            using (var connectorClient = await BotConnectorUtility.BuildConnectorClientAsync(context.Activity.ServiceUrl))
            {
                var channelInfo = GetHardcodedChannelId();

                AdaptiveCard card = new AdaptiveCard();
                card.Body.Add(new AdaptiveTextBlock
                {
                    Text = $"New research request from {context.Activity.From.Name}. VSO:{vsoTicketNumber}",
                    Size = AdaptiveTextSize.Large,
                    Wrap = true,
                    Separator = true
                });
                card.Body.Add(summary);
                card.Actions.Add(new AdaptiveOpenUrlAction()
                {
                    Url = new Uri($"{Uri}/{Project}/_workitems/edit/{vsoTicketNumber}"),
                    Title = $"Vso: {vsoTicketNumber}"
                });

                context.ConversationData.SetValue("VsoId", vsoTicketNumber);
                context.ConversationData.SetValue("EndUserConversationId", context.Activity.Conversation.Id);

                var conversationResourceResponse = await ConversationHelpers.CreateAgentConversation(channelInfo,
                    card,
                    $"New research request from {context.Activity.Recipient.Name}",
                    connectorClient,
                    vsoTicketNumber,
                    context.Activity as IMessageActivity);

                EndUserAndAgentConversationMappingState state =
                    new EndUserAndAgentConversationMappingState(vsoTicketNumber.ToString(),
                        context.Activity.From.Name,
                        context.Activity.From.Id,
                        context.Activity.Conversation.Id,
                        conversationResourceResponse.Id);

                await state.SaveInVso(vsoTicketNumber.ToString());
            }

            AdaptiveCard responseCard = new AdaptiveCard();
            responseCard.Body.Add(new AdaptiveTextBlock
            {
                Text = "Thank you! I have posted following to experts. " +
                       "I will be in touch with you shortly. " +
                       $"Please use reference #{vsoTicketNumber} for this request in future.",
                Size = AdaptiveTextSize.Default,
                Wrap = true,
                Separator = true
            });
            summary.Facts.Add(new AdaptiveFact("Reference #", vsoTicketNumber.ToString()));
            responseCard.Body.Add(summary);

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

            context.Done<object>(null);
        }

        private static ChannelInfo GetHardcodedChannelId()
        {
            return new ChannelInfo("19:c20b196747424d8db51f6c00a8a9efa8@thread.skype", "Research Agents");
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