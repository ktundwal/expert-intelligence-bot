using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using com.microsoft.ExpertConnect.Helpers;
using com.microsoft.ExpertConnect.Models;
using DriveItem = Microsoft.Graph.DriveItem;

namespace com.microsoft.ExpertConnect.Dialogs
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

        public static DriveItem UploadAnItemToOneDrive(TokenResponse tokenResponse, string style, string emailToShareWith = "nightking@expertconnectdev.onmicrosoft.com")
        {
            DriveItem uploadedItem = null;
            if (tokenResponse != null)
            {
                var client = GraphClient.GetAuthenticatedClient(tokenResponse.Token);
                var folder = GraphClient.GetOrCreateFolder(client, "expert-connect").Result;
                uploadedItem = GraphClient.UploadPowerPointFileToDrive(client, folder, style);
                if (!string.IsNullOrEmpty(emailToShareWith))
                {
                    var shareWithResponse = GraphClient.ShareFileAsync(
                        client, 
                        uploadedItem, 
                        emailToShareWith, 
                        "sharing via OneDriveClient").Result;
                }
            }

            return uploadedItem;
        }
    }
}
