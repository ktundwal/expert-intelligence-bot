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
        private readonly string _name;
        private readonly string _description;
        private readonly string _additionalInfoFromUser;
        private readonly DateTime _deadline;
        private readonly UserProfile _userProfile;
        private readonly bool _isSms;

        public ConfirmResearchRequestDialog(bool isSms,
            string name,
            string description,
            string additionalInfoFromUser,
            DateTime deadline,
            UserProfile userProfile)
        {
            this._isSms = isSms;
            this._name = name;
            this._description = description;
            this._additionalInfoFromUser = additionalInfoFromUser;
            this._deadline = deadline;
            this._userProfile = userProfile;
        }

        public async Task StartAsync(IDialogContext context)
        {
            IMessageActivity responseMessage = _isSms ? BuildConfirmationMessageForSms(context) :
            BuildConfirmationMessageForTeams(context);

            await context.PostWithRetryAsync(responseMessage);
            context.Wait(OnConfirmationAsync);
        }

        private IMessageActivity BuildConfirmationMessageForSms(IDialogContext context)
        {
            var responseMessage = context.MakeMessage();
            responseMessage.Text = "Okay, here is what I will send to the freelancer.\n\n\n\n" +
                                   $"Who: {_userProfile}\n\n" +
                                   $"What: {_description}\n\n" +
                                   $"Additional Info: {_additionalInfoFromUser}\n\n" +
                                   $"When: {_deadline}\n\n\n\n" +
                                   $"Shall I send this to freelancer now? You can say 'yes' or 'no'";
            responseMessage.TextFormat = "plain";
            return responseMessage;
        }

        private IMessageActivity BuildConfirmationMessageForTeams(IDialogContext context)
        {
            var summary = new AdaptiveFactSet
            {
                Facts = new List<AdaptiveFact>
                {
                    new AdaptiveFact("Who", context.Activity.From.Name),
                    new AdaptiveFact("What", _description),
                    new AdaptiveFact("Additional Info", _additionalInfoFromUser),
                    new AdaptiveFact("When", _deadline.ToString()),
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
            return responseMessage;
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