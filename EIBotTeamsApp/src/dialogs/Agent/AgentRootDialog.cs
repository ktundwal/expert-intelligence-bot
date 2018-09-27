using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Scorables;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;
using Microsoft.Teams.TemplateBotCSharp.Dialogs;

namespace Microsoft.Office.EIBot.Service.dialogs.Agent
{
    /// <summary>
    /// This is Root Dialog, its a triggring point for every Child dialog based on the RexEx Match with user input command
    /// </summary>

    [Serializable]
    public class AgentRootDialog : DispatchDialog
    {
        #region Reply to user

        [RegexPattern(DialogMatches.ReplyToUser)]
        [ScorableGroup(1)]
        public void ReplyToUser(IDialogContext context, IActivity activity)
        {
            context.Call(new ReplyToUserDialog(), this.EndDefaultDialog);
        }

        #endregion

        #region Help Dialog

        [RegexPattern(DialogMatches.Help)]
        [ScorableGroup(1)]
        public async Task Help(IDialogContext context, IActivity activity)
        {
            await Default(context, activity);
        }

        [MethodBind]
        [ScorableGroup(2)]
        public async Task Default(IDialogContext context, IActivity activity)
        {
            await SaveAgentChannelIdInAzureStore(context);
            await SaveBotIdInAzureStorage(context);

            // grab the channel ids here and store in our table store
            context.Call(new AgentHelpDialog(), this.EndDefaultDialog);
        }

        private async Task SaveBotIdInAzureStorage(IDialogContext context)
        {
            try
            {
                if (context.Activity.Recipient.Name.Equals(ConfigurationManager.AppSettings["BotName"]))
                {
                    await IdTable.SetBotId(context.Activity.Recipient);
                }
            }
            catch (System.Exception e)
            {
                Trace.TraceError($"Error setting bot id. {e}");
            }
        }

        private static async Task SaveAgentChannelIdInAzureStore(IDialogContext context)
        {
            try
            {
                using (var connectorClient = await BotConnectorUtility.BuildConnectorClientAsync(context.Activity.ServiceUrl))
                {
                    var ci = GetChannelId(connectorClient, context, ConfigurationManager.AppSettings["AgentChannelName"]);
                    await IdTable.SetAgentChannel(ci.Name, ci.Id);
                    WebApiConfig.TelemetryClient.TrackEvent("SaveAgentChannelIdInAzureStore", 
                        new Dictionary<string, string>{{ ci.Name, ci.Id}});
                    Trace.TraceInformation($"Id of {ci.Name}' is {ci.Id}.");
                }
            }
            catch (System.Exception e)
            {
                Trace.TraceError($"Error getting channel id. {e}");
            }
        }

        public Task EndDefaultDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done<object>(null);
            return Task.CompletedTask;
        }

        public Task EndDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done<object>(null);
            return Task.CompletedTask;
        }

        #endregion

        #region Team Info

        [RegexPattern(DialogMatches.TeamInfo)]
        [ScorableGroup(1)]
        public void TeamsInfo(IDialogContext context, IActivity activity)
        {
            context.Call(new FetchTeamsInfoDialog(), this.EndDialog);
        }

        #endregion

        private static ChannelInfo GetChannelId(ConnectorClient connectorClient, IDialogContext context, string channelName)
        {
            var teamInfo = context.Activity.GetChannelData<TeamsChannelData>().Team;
            ConversationList channels = connectorClient.GetTeamsConnectorClient().Teams.FetchChannelList(teamInfo.Id);
            var channelInfo = channels.Conversations.FirstOrDefault(c => c.Name != null && c.Name.Equals(channelName));
            if (channelInfo == null) throw new System.Exception($"{channelName} doesn't exist in {context.Activity.GetChannelData<TeamsChannelData>().Team.Name} Team");
            return channelInfo;
        }
    }
}