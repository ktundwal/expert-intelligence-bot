using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using PPTExpertConnect.Helpers;
using PPTExpertConnect.Models;

namespace PPTExpertConnect.Dialogs
{
    public class DialogHelper
    {
        public static UserInfo GetUserInfoFromContext(WaterfallStepContext step)
        {
            var result = step.Options as UserInfo ?? new UserInfo();

            return result;
        }
        public static PromptOptions CreateAdaptiveCardAsPrompt(AdaptiveCard card)
        {
            return new PromptOptions
            {
                Prompt = (Activity)MessageFactory.Attachment(CreateAdaptiveCardAttachment(card))
            };
        }
        public static Attachment CreateAdaptiveCardAttachment(AdaptiveCard card)
        {
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(card)),
            };
            return adaptiveCardAttachment;
        }

        public static IActivity CreateAdaptiveCardAsActivity(AdaptiveCard card)
        {
            return (Activity)MessageFactory.Attachment(CreateAdaptiveCardAttachment(card));
        }

        public static async Task PostLearningContentAsync(ITurnContext context, CardBuilder cb, CancellationToken cancellationToken)
        {
            await context.SendActivityAsync(
                CreateAdaptiveCardAsActivity(
                    cb.V2Learning(
                        "Great. Will you be presenting this during a meeting? If so, we recommend checking out this LinkedIn Learning course on how to deliver and effective presentation:",
                        "https://www.linkedin.com/",
                        null,
                        "PowerPoint Tips and Tricks for Business Presentations"
                    )
                ),
                cancellationToken);
        }

    }
}
