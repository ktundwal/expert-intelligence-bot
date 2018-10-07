using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Office.EIBot.Service.Properties;
using Microsoft.Office.EIBot.Service.utility;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    [Serializable]
    public class ConfirmResearchRequestDialog : IDialog<bool>
    {
        private string name;
        private string description;
        private string additionalInfoFromUser;
        private DateTime deadline;

        public ConfirmResearchRequestDialog(string name, string description, string additionalInfoFromUser, DateTime deadline)
        {
            this.name = name;
            this.description = description;
            this.additionalInfoFromUser = additionalInfoFromUser;
            this.deadline = deadline;
        }

        public async Task StartAsync(IDialogContext context)
        {
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

            AdaptiveCard responseCard = new AdaptiveCard();
            responseCard.Body.Add(new AdaptiveTextBlock
            {
                Text = "Okay, here is what I will send to the freelancer.",
                Size = AdaptiveTextSize.Default,
                Wrap = true,
                Separator = true
            });
            responseCard.Body.Add(summary);

            var responseMessage = context.MakeMessage();
            responseMessage.Attachments = new List<Attachment>
            {
                new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = responseCard
                },
                new HeroCard{Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, "Send it", value: "yes"),
                    }
                }.ToAttachment()
            };

            await context.PostWithRetryAsync(responseMessage);
            context.Wait(OnConfirmationAsync);
        }

        private async Task OnConfirmationAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException((nameof(result)) + Strings.NullException);
            }

            var confirmationResult = await result;
            if (confirmationResult.Text == "yes")
            {
                context.Done(true);
            }
            else if (confirmationResult.Text == "no")
            {
                context.Done(false);
            }
            else
            {
                await context.PostWithRetryAsync("Sorry, I didn't get that. Please say 'yes' to send or 'no' to cancel");
                context.Wait(OnConfirmationAsync);
            }
        }
    }
}