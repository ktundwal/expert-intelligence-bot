using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
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
    public class ExpertConnectDialog : IDialog<object>
    {
        private const string DescriptionKey = "description";
        private const string AliasKey = "alias";
        private const string VsoIdKey = "VsoId";
        private const string EndUserConversationIdKey = "EndUserConversationId";
        private bool _isSms;
        private readonly int MinAliasCharLength = 3;
        private readonly int MinDescriptionCharLength = 6;
        private int _minHoursToCompleteResearch;
        static readonly string Uri = ConfigurationManager.AppSettings["VsoOrgUrl"];
        static readonly string Project = ConfigurationManager.AppSettings["VsoProject"];

        public async Task StartAsync(IDialogContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _isSms = context.Activity.ChannelId == ActivityHelper.SmsChannelId;
            _minHoursToCompleteResearch = Convert.ToInt32(ConfigurationManager.AppSettings["ResearchProjectViaTeamsMinHours"]);

            if (await ConversationHelpers.RelayMessageToAgentIfThereIsAnOpenResearchProject(context))
            {
                context.Done<object>(null);
            }
            else
            {
                // check we know alias associated with this phone number
                UserTable userTable = new UserTable();
                var botUsers = await userTable.GetUserByChannelSpecificId(context.Activity.ChannelId, context.Activity.From.Id);
                if (botUsers.Length == 0)
                {
                    context.Call(new PromptText(
                        "Hi, I’m here to help you get started. \n\n" +
                        "When we're done, I'll email you a research report with the info you want.\n\n" +
                        "What's your Microsoft alias?",
                        $"Please try again. Alias needs to be at least {MinAliasCharLength} characters long.",
                        "Sorry, I didn't get that. too many attempts. Please try again later.", 2, MinAliasCharLength), OnAliasReceivedAsync);
                }
                else
                {
                    // we have the alias
                    var user = botUsers.First();
                    context.UserData.SetValue(UserProfileHelper.UserProfileKey, user);
                    context.Call(new PromptText(
                        $"Hey {user.Alias}, let's get started. \n\n" +
                        "Tell me what you want to know, and I'll kick off a research project.\n\n" +
                        "OR\n\n" +
                        "Say 'example' and I'll show you some good (and bad) research requests.",
                        $"Please try again. Response needs to be at least {MinDescriptionCharLength} characters long.",
                        "Sorry, I didn't get that. too many attempts. Please try again later.", 2, MinDescriptionCharLength),
                        OnDescriptionOrExampleRequestReceivedAsync);
                }
            }
        }

        private async Task OnAliasReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var alias = await result;
            context.UserData.SetValue(AliasKey, alias);
            UserProfile user = await StoreInUserTable(context);
            context.UserData.SetValue(UserProfileHelper.UserProfileKey, user);

            context.Call(new PromptText(
                "Got it. Now, tell me what you want to know, and I'll kick off a research project. \n\nOR \n\n" +
                "Say 'example' and I'll show you some good (and bad) research requests.",
                    $"Please try again. Response needs to be at least {MinDescriptionCharLength} characters long.",
                    "Sorry, I didn't get that. too many attempts. Please try again later.", 2, MinDescriptionCharLength),
                OnDescriptionOrExampleRequestReceivedAsync);
        }

        private async Task OnDescriptionOrExampleRequestReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var descriptionFromUser = await result;
            if (descriptionFromUser.ToLower().Trim().Replace("'","") == "example")
            {
                context.Call(new PromptText("Here you go. \n\n" +
                                        "Good: \n\n" +
                                        "What are the top 5 gig economy platforms in Singapore?\n\n" +
                                        "What are the top 10 browsers focused on privacy? Include funding, traction, and target users\n\n" +
                                        "Bad: \n\n" +
                                        "What's a good app?",
                                        $"Please try again. Response needs to be at least {MinDescriptionCharLength} characters long.",
                                        "Sorry, I didn't get that. too many attempts. Please try again later.", 2, MinDescriptionCharLength),
                    OnDescriptionReceivedAsync);
            }
            else
            {
                context.ConversationData.SetValue(DescriptionKey, descriptionFromUser);
                await CreateProject(context);
            }
        }

        private async Task OnDescriptionReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            ThrowExceptionIfResultIsNull(result);

            var descriptionFromUser = await result;
            context.ConversationData.SetValue(DescriptionKey, descriptionFromUser);
            await CreateProject(context);
        }

        private async Task CreateProject(IDialogContext context)
        {
            try
            {
                var description = context.ConversationData.GetValue<string>(DescriptionKey);
                var userProfile = context.UserData.GetValue<UserProfile>(UserProfileHelper.UserProfileKey);
                var deadline = DateTime.UtcNow.AddHours(_minHoursToCompleteResearch);

                var vsoTicketNumber = await VsoHelper.CreateTaskInVso(VsoHelper.ResearchTaskType,
                    context.Activity.From.Name,
                    description,
                    ConfigurationManager.AppSettings["AgentToAssignVsoTasksTo"],
                    deadline,
                    "",
                    userProfile,
                    context.Activity.ChannelId);

                context.ConversationData.SetValue(VsoIdKey, vsoTicketNumber);
                context.ConversationData.SetValue(EndUserConversationIdKey, context.Activity.Conversation.Id);

                var conversationTitle = $"Web research request from {userProfile} via {context.Activity.ChannelId} due {deadline}";
                string agentConversationId = await ConversationHelpers.CreateAgentConversationEx(context,
                    conversationTitle,
                    CreateCardForAgent(context,
                        description,
                        deadline,
                        vsoTicketNumber,
                        userProfile),
                    userProfile);

                EndUserAndAgentConversationMappingState state =
                    new EndUserAndAgentConversationMappingState(vsoTicketNumber.ToString(),
                        context.Activity.From.Name,
                        context.Activity.From.Id,
                        context.Activity.Conversation.Id,
                        agentConversationId);

                await state.SaveInVso(vsoTicketNumber.ToString());

                WebApiConfig.TelemetryClient.TrackEvent("CreateProject", new Dictionary<string, string>()
                {
                    {"from", context.Activity.From.Name },
                    {UserProfileHelper.UserProfileKey, userProfile.ToString() },
                    {DescriptionKey, description },
                    {"deadline", deadline.ToString() },
                    {VsoIdKey, vsoTicketNumber.ToString()},
                });

                await context.PostWithRetryAsync($"OK, I've created Project {vsoTicketNumber} for you. " +
                                                 "We'll get to work on this shortly and send you a confirmation email. " +
                                                 "In the meantime, feel free to tell me more. " +
                                                 "Like: what do you want to do with this info ?");

                context.Done<object>(true);
            }
            catch (System.Exception e)
            {
                try
                {
                    if (context.ConversationData.TryGetValue(VsoIdKey, out string vsoTicketNumber))
                    {
                        // close this ticket
                        await VsoHelper.CloseProject(Convert.ToInt32(vsoTicketNumber));
                    }
                }
                catch (System.Exception exception)
                {
                    WebApiConfig.TelemetryClient.TrackException(exception, new Dictionary<string, string>
                    {
                        {"debugNote", "Error closing project during exception received in CreateProject" },
                        {"CreateProjectException", e.ToString() },
                    });
                }
                await context.PostWithRetryAsync("Sorry, I ran into an issue while connecting with agent. Please try again later.");
                WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string> { { "function", "OnConfirmResearchDialog.CreateAgentConversation" } });
                context.Done<object>(false);
            }
        }

        private static void ThrowExceptionIfResultIsNull(IAwaitable<object> result)
        {
            if (result == null)
            {
                throw new InvalidOperationException(nameof(result) + Strings.NullException);
            }
        }

        private AdaptiveCard CreateCardForAgent(
            IDialogContext context,
            string description,
            DateTime deadline,
            int vsoTicketNumber,
            UserProfile userProfile)
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
            card.Body.Add(new AdaptiveTextBlock { Text = "- Please post jobs to UpWork manually. " +
                                                         "Support to posting via bot is coming soon.", Wrap = true });
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = "- Sending attachments is not supported. " +
                       "Please send research documents as a **link**. " +
                       "Upload file in 'files' tab, use it to go to SharePoint site. " +
                       "From there 'Share > Email'. " +
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

        private async Task<UserProfile> StoreInUserTable(IBotContext context)
        {
            UserTable userTable = new UserTable();
            return await userTable.AddUser(
                context.Activity.ChannelId,
                context.Activity.From.Id,
                string.Empty, //name
                _isSms ? context.Activity.From.Id : string.Empty,
                context.UserData.GetValue<string>(AliasKey));
        }
    }
}