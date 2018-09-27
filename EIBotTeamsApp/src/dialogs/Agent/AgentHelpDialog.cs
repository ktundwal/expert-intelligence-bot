using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Office.EIBot.Service.Properties;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service.dialogs.Agent
{
    /// <summary>
    /// This is Help Dialog Class. Main purpose of this dialog class is to post the help commands in Teams.
    /// These are Actionable help commands for easy to use.
    /// </summary>
    [Serializable]
    public class AgentHelpDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            WebApiConfig.TelemetryClient.TrackEvent("AgentHelpDialog", new Dictionary<string, string>
            {
                {"class", "AgentHelpDialog" },
                {"function", "StartAsync" },
                {"from", context.Activity.From.Name }
            });

            var message = context.MakeMessage();

            // This will create Interactive Card with help command buttons
            message.Attachments = new List<Attachment>
            {
                new HeroCard(Strings.HelpTitle)
                {
                    Text = "Do you want to talk to EIBot dev team?",
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, "yes", value: "Yes"),
                        new CardAction(ActionTypes.ImBack, "no", value: "No")
                    }
                }.ToAttachment()
            };

            await context.PostWithRetryAsync(message);

            context.Wait(MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result; // We've got a message!
            if (message.Text.ToLower().Contains("yes"))
            {
                await context.PostWithRetryAsync("Sure, pls send a message to dev team.");
            }
            if (message.Text.ToLower().Contains("no"))
            {
                await context.PostWithRetryAsync("Sure. Thanks for trying out my functionality");
            }

            context.Done<object>(null);
        }
    }
}