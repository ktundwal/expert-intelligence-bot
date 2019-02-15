using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using com.microsoft.ExpertConnect.Helpers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Bot.Schema;
using Microsoft.ExpertConnect.Helpers;
using Microsoft.ExpertConnect.Models;
using Newtonsoft.Json;
using DriveItem = Microsoft.Graph.DriveItem;

namespace Microsoft.ExpertConnect.Dialogs
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
                Prompt = MessageFactory.Attachment(CreateAdaptiveCardAttachment(card)) as Activity,
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

        public static async Task CreateProjectAndSendToUserAndAgent(ITurnContext context, UserInfo userInfo, CardBuilder cb, VsoHelper vso, SimpleCredentialProvider credentials, IdTable idTable, EndUserAndAgentIdMapping endUserAndAgentTable)
        {
            var ticketNumber = await vso.CreateProject(context, userInfo);
            if (ticketNumber == int.MinValue)
            {
                throw new System.Exception("rsadad");
            }
            var cardToSend = cb.V2VsoTicketCard(ticketNumber, "https://www.microsoft.com");

            await context.SendActivityAsync(CreateAdaptiveCardAsActivity(cardToSend));

            var agentConversationId = await CreateAgentConversationMessage(
                context,
                $"PowerPoint request from {context.Activity.From.Name} via {context.Activity.ChannelId}",
                credentials,
                idTable,
                cardToSend);

            var endUserMapping = await endUserAndAgentTable.CreateNewMapping(
                ticketNumber.ToString(), // Obtain this information from userInfo Class
                context.Activity.From.Name,
                context.Activity.From.Id,
                JsonConvert.SerializeObject(context.Activity.GetConversationReference()),
                agentConversationId);

            await endUserAndAgentTable.SaveInVso(
                ticketNumber.ToString(),
                vso,
                endUserMapping);
        }

        private static async Task<string> CreateAgentConversationMessage(ITurnContext context, string topicName, SimpleCredentialProvider credentials, IdTable idTable, AdaptiveCard cardToSend)
        {
            var serviceUrl = context.Activity.ServiceUrl;
            var agentChannelInfo = await idTable.GetAgentChannelInfo();
            ChannelAccount botMsTeamsChannelAccount = await idTable.GetBotId();

            var connectorClient =
                BotConnectorUtility.BuildConnectorClientAsync(
                    credentials.AppId,
                    credentials.Password,
                    serviceUrl);

            try
            {
                var channelData = new TeamsChannelData { Channel = agentChannelInfo, Notification = new NotificationInfo(true) };

                IMessageActivity agentMessage = Activity.CreateMessageActivity();
                agentMessage.From = botMsTeamsChannelAccount;
                //                agentMessage.Recipient =
                //                    new ChannelAccount(ConfigurationManager.AppSettings["AgentToAssignVsoTasksTo"]);
                agentMessage.Type = ActivityTypes.Message;
                agentMessage.ChannelId = Constants.MsTeamsChannelId;
                agentMessage.ServiceUrl = serviceUrl;

                agentMessage.Attachments = new List<Attachment>
                {
                    new Attachment {ContentType = AdaptiveCard.ContentType, Content = cardToSend},
                };

                var agentMessageActivity = (Activity)agentMessage;

                ConversationParameters conversationParams = new ConversationParameters(
                    isGroup: true,
                    bot: null,
                    members: null,
                    topicName: topicName,
                    activity: agentMessageActivity,
                    channelData: channelData);

                var conversationResourceResponse = await BotConnectorUtility.BuildRetryPolicy().ExecuteAsync(
                    async ()
                        => await connectorClient.Result.Conversations.CreateConversationAsync(conversationParams));

                return conversationResourceResponse.Id;
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
                throw;
            }
        }
    }
}
