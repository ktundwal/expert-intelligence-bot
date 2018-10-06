using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Office.EIBot.Service.dialogs.EndUser;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;

public class UserProfileKeys
{
    public const string AliasKey = "Alias";
    public const string NameKey = "Name";
    public const string PhoneKey = "Phone";
}

// https://github.com/Microsoft/BotBuilder-V3/blob/master/CSharp/Tests/Microsoft.Bot.Builder.Tests/ChainTests.cs
public class AskAliasDialog : IDialog<string>
{
    private readonly IDialog _nextDialogToCall;

    public AskAliasDialog(IDialog nextDialogToCall)
    {
        _nextDialogToCall = nextDialogToCall;
    }

    public static IDialog<string> MakeSelectManyQuery()
    {
        var prompts = new[] { "p1", "p2", "p3" };

        var query = from x in new PromptDialog.PromptString(prompts[0], prompts[0], attempts: 1)
            from y in new PromptDialog.PromptString(prompts[1], prompts[1], attempts: 1)
            from z in new PromptDialog.PromptString(prompts[2], prompts[2], attempts: 1)
            select string.Join(" ", x, y, z);

        query = query.PostToUser();

        return query;
    }

    public Task StartAsync(IDialogContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var promptText = new PromptText(
            "Okay, since this is your first freelancer request, can you please tell us your Microsoft alias?",
            "Please try again", "Wrong again. Too many attempts.", 2, 2);
        context.Call(promptText, OnAliasReceivedAsync);
    }

    private async Task OnAliasReceivedAsync(IDialogContext context, IAwaitable<string> result)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var alias = await result;
        WebApiConfig.TelemetryClient.TrackEvent("PromptForAlias", new Dictionary<string, string>
        {
            {"name",  context.Activity.From.Name
            },
            {"alias", alias }
        });
        context.UserData.SetValue(UserProfileKeys.AliasKey, alias);

        context.Call(_nextDialogToCall, OnNextDialogReturn);
    }

    private Task OnNextDialogReturn(IDialogContext context, IAwaitable<object> result)
    {
        context.Done();
    }
}

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    [Serializable]
    public class UserProfileDialog : IDialog<UserProfile>
    {
        private const string AliasKey = "Alias";
        private const string NameKey = "Name";
        private const string PhoneKey = "Phone"; 

        public async Task StartAsync(IDialogContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            WebApiConfig.TelemetryClient.TrackEvent("UserProfileDialog", new Dictionary<string, string>
            {
                {"class", "UserProfileDialog" },
                {"function", "StartAsync" },
                {"from", context.Activity.From.Name }
            });

            UserTable userTable = new UserTable();
            var botUsers = await userTable.GetUserByChannelSpecificId(context.Activity.ChannelId, context.Activity.From.Id);
            if (botUsers.Length == 0)
            {
                
                switch (context.Activity.ChannelId)
                {
                    case ActivityHelper.SmsChannelId:
                        context.UserData.SetValue(PhoneKey, context.Activity.From.Id);  // if SMS, id is same as phone
                        PromptForAliasAndName(context);
                        break;
                    case ActivityHelper.MsTeamChannelId:
                        context.UserData.SetValue(NameKey, context.Activity.From.Name);   // if teams, name has the correct name
                        var promptText = new PromptText(
                            "Okay, since this is your first freelancer request, can you please tell us your Microsoft alias?",
                            "Please try again", "Wrong again. Too many attempts.", 2, 2);
                        context.Call(promptText, OnAliasReceivedAsync);
                        break;
                    default:
                        throw new System.Exception("Unsupported channel");
                }
            }
            else
            {
                context.Done(botUsers.First());
            }
        }

        

        private async Task OnPhoneReceivedAsync(IDialogContext phoneDialogContext, IAwaitable<string> phoneResult)
        {
            if (phoneDialogContext == null)
            {
                throw new ArgumentNullException(nameof(phoneDialogContext));
            }

            var phone = await phoneResult;
            WebApiConfig.TelemetryClient.TrackEvent("PromptForPhone", new Dictionary<string, string>
            {
                {"name",  phoneDialogContext.Activity.From.Name
                },
                {"phone", phone }
            });

            phoneDialogContext.UserData.SetValue(PhoneKey, phone);

            UserProfile userProfile = await StoreInUserTable(phoneDialogContext);

            phoneDialogContext.Done(userProfile);
        }

        private void PromptForAliasAndName(IDialogContext context)
        {
            context.Call(new PromptText(
                    "Okay, since this is your first freelancer request, can you please tell us your name?",
                    "Please try again", "Wrong again. Too many attempts.", 2, 2), OnNameReceivedAsync);
        }

        private async Task OnNameReceivedAsync(IDialogContext nameDialogContext, IAwaitable<string> nameResult)
        {
            if (nameDialogContext == null)
            {
                throw new ArgumentNullException(nameof(nameDialogContext));
            }

            var name = await nameResult;
            WebApiConfig.TelemetryClient.TrackEvent("PromptForName", new Dictionary<string, string>
            {
                {"id",  nameDialogContext.Activity.From.Id
                },
                {"name", name }
            });
            nameDialogContext.UserData.SetValue(NameKey, name);
            nameDialogContext.Call(new PromptText(
                    "Can you also please tell us your Microsoft alias?  That way we can reach you by email if need to.",
                    "Please try again", "Wrong again. Too many attempts.", 2, 10), OnAliasReceivedAfterNameAsync);
        }

        private async Task OnAliasReceivedAfterNameAsync(IDialogContext aliasDialogContext, IAwaitable<string> aliasResult)
        {
            if (aliasDialogContext == null)
            {
                throw new ArgumentNullException(nameof(aliasDialogContext));
            }

            var alias = await aliasResult;
            WebApiConfig.TelemetryClient.TrackEvent("PromptForAlias", new Dictionary<string, string>
            {
                {"name",  aliasDialogContext.Activity.From.Name
                },
                {"alias", alias }
            });
            aliasDialogContext.UserData.SetValue(AliasKey, alias);

            UserProfile userProfile = await StoreInUserTable(aliasDialogContext);

            aliasDialogContext.Done(userProfile);
        }

        private async Task<UserProfile> StoreInUserTable(IBotContext context)
        {
            UserTable userTable = new UserTable();
            return await userTable.AddUser(
                context.Activity.ChannelId,
                context.Activity.From.Id,
                context.UserData.GetValue<string>(NameKey),
                context.UserData.GetValue<string>(PhoneKey),
                context.UserData.GetValue<string>(AliasKey));
        }
    }
}