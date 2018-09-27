using System;
using System.Collections.Generic;
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
    [Serializable]
    public class UserSmsDefaultDialog : IDialog<object>
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
                await context.PostWithRetryAsync("Hi, this is EIBot. I can help with many todos. For instance, booking an appointment. " +
                                        "Lets start with what do you need?");
                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException((nameof(result)) + Strings.NullException);
            }

            //Prompt the user with welcome message before game starts
            IMessageActivity message = await result;

            await context.PostWithRetryAsync($"I can help with {message.Text}.");

            // Store description
            context.ConversationData.SetValue("description", message.Text);

            // Prompt for delivery date
            var prompt = new DeadlinePrompt(GetCurrentCultureCode());
            context.Call(prompt, this.OnDeadlineSelected);
        }

        private async Task OnDeadlineSelected(IDialogContext context, IAwaitable<IEnumerable<DateTime>> result)
        {
            try
            {
                // "result" contains the date (or array of dates) returned from the prompt
                IEnumerable<DateTime> momentOrRange = await result;
                var deadline = momentOrRange.First(); // DeadlinePrompt.MomentOrRangeToString(momentOrRange);

                // Store date
                context.ConversationData.SetValue("deadline", deadline);

                var description = context.ConversationData.GetValue<string>("description");

                var vsoTicketNumber = await VsoHelper.CreateTaskInVso(VsoHelper.VirtualAssistanceTaskType,
                    context.Activity.From.Name,
                    description,
                    "mamottol@microsoft.com",
                    deadline,
                    "");

                MicrosoftAppCredentials.TrustServiceUrl(ActivityHelper.TeamsServiceEndpoint);

                AdaptiveCard card = new AdaptiveCard();
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = $"New Virtual Assistance request from {context.Activity.From.Name}. VSO:{vsoTicketNumber}",
                    Size = AdaptiveTextSize.Large,
                    Wrap = true,
                    Separator = true
                });
                var summary = new AdaptiveFactSet
                {
                    Facts = new List<AdaptiveFact>
                    {
                        new AdaptiveFact("Who", context.Activity.From.Name),
                        new AdaptiveFact("What", description),
                        new AdaptiveFact("When", deadline.ToString()),
                        new AdaptiveFact("Vso", vsoTicketNumber.ToString()),
                    }
                };
                card.Body.Add(summary);

                using (var connectorClient = await BotConnectorUtility.BuildConnectorClientAsync(ActivityHelper.TeamsServiceEndpoint))
                {
                    var channelInfo = GetHardcodedChannelId();
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

                await context.PostWithRetryAsync("Thank you! I have posted following to internal agents. " +
                                                 "I will be in touch with you shortly. " +
                                                 $"Please use reference #{vsoTicketNumber} for this request in future. " +
                                                 $"What: {description}. When: {deadline}.");

                context.Done<object>(null);
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
    }
}