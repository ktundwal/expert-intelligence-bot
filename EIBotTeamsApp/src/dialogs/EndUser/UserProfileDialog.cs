using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;

// https://github.com/Microsoft/BotBuilder-V3/blob/master/CSharp/Tests/Microsoft.Bot.Builder.Tests/ChainTests.cs

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    [Serializable]
    public class UserProfileDialog : IDialog<UserProfile>
    {
        private const string EmailKey = "Email";
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
                        context.UserData.SetValue(PhoneKey, PromptPhoneNumber.FormatPhoneNumber(context.Activity.From.Id));  // if SMS, id is same as phone
                        PromptForNameAndAlias(context);
                        break;
                    case ActivityHelper.MsTeamChannelId:
                        context.UserData.SetValue(NameKey, context.Activity.From.Name);   // if teams, name has the correct name
                        PromptForAliasAndMobilePhone(context);
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

        #region PromptForAliasAndMobilePhone

        /// <summary>
        /// This is for users coming via Teams.
        /// todo: look this up from Microsoft graph
        /// </summary>
        /// <param name="context"></param>
        private void PromptForAliasAndMobilePhone(IDialogContext context)
        {
            context.Call(new PromptEmail(
                "Okay, since this is your first freelancer request, can you please tell us your Microsoft email?",
                "Please try again", "Wrong again. Too many attempts.", 2), OnEmailReceivedAsync);
        }

        /// <summary>
        /// This will call phone prompt afterwards
        /// </summary>``````
        /// <param name="emailDialogContext"></param>
        /// <param name="emailResult"></param>
        /// <returns></returns>
        private async Task OnEmailReceivedAsync(IDialogContext emailDialogContext, IAwaitable<string> emailResult)
        {
            if (emailResult == null)
            {
                throw new ArgumentNullException(nameof(emailResult));
            }

            string email = await ProcessEmailResponse(emailDialogContext, emailResult);

            WebApiConfig.TelemetryClient.TrackEvent("PromptForAlias", new Dictionary<string, string>
            {
                {"name",  emailDialogContext.Activity.From.Name},
                {"email", email }
            });
            emailDialogContext.UserData.SetValue(EmailKey, email);

            emailDialogContext.Call(new PromptPhoneNumber(
                "Can you also please tell us your mobile phone number?  That way you can reach us via SMS as well.",
                "Please try again", "Wrong again. Too many attempts."), OnPhoneReceivedAsync);
        }

        private static async Task<string> ProcessEmailResponse(IDialogContext emailDialogContext, IAwaitable<string> emailResult)
        {
            string email = "Not available";
            try
            {
                email = await emailResult;
            }
            catch (TooManyAttemptsException)
            {
                await emailDialogContext.PostWithRetryAsync("Sorry, I had trouble understanding. " +
                                                            "Lets proceed. Project manager will clarify email later.");
            }

            return email;
        }

        private async Task OnPhoneReceivedAsync(IDialogContext phoneDialogContext, IAwaitable<string> phoneResult)
        {
            if (phoneResult == null)
            {
                throw new ArgumentNullException(nameof(phoneResult));
            }

            string phone = await ProcessPhoneNumberResponse(phoneDialogContext, phoneResult);

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

        private static async Task<string> ProcessPhoneNumberResponse(IDialogContext phoneDialogContext, IAwaitable<string> phoneResult)
        {
            string phone = "Not available";
            try
            {
                phone = await phoneResult;
            }
            catch (TooManyAttemptsException)
            {
                await phoneDialogContext.PostWithRetryAsync("Sorry, I had trouble understanding. " +
                                                            "Lets proceed. Project manager will clarify phone number later.");
            }

            return phone;
        }

        #endregion

        #region PromptForNameAndAlias

        /// <summary>
        /// this is for users coming via SMS channel
        /// </summary>
        /// <param name="context"></param>

        private void PromptForNameAndAlias(IDialogContext context)
        {
            context.Call(new PromptEmail(
                    "Okay, since this is your first freelancer request, can you please tell us your Microsoft email?",
                    "Please try email again", "Sorry I couldn't understand email. Too many attempts.", 2), OnEmailReceivedOverSmsAsync);
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
                    "Please try again", "Wrong again. Too many attempts.", 2, 10), OnEmailReceivedOverSmsAsync);
        }

        private async Task OnEmailReceivedOverSmsAsync(IDialogContext emailDialogContext, IAwaitable<string> emailResult)
        {
            if (emailDialogContext == null)
            {
                throw new ArgumentNullException(nameof(emailDialogContext));
            }

            string email = await ProcessEmailResponse(emailDialogContext, emailResult);
            WebApiConfig.TelemetryClient.TrackEvent("PromptForAlias", new Dictionary<string, string>
            {
                {"name",  emailDialogContext.Activity.From.Name
                },
                {"email", email }
            });
            emailDialogContext.UserData.SetValue(EmailKey, email);
            emailDialogContext.UserData.SetValue(NameKey, ParseAliasFromEmail(email)); // on SMS we are not going to ask name. Use alias instead

            UserProfile userProfile = await StoreInUserTable(emailDialogContext);

            emailDialogContext.Done(userProfile);
        }

        private string ParseAliasFromEmail(string email)
        {
            try
            {
                return new MailAddress(email).User;
            }
            catch (System.Exception)
            {
                return "";
            }
        }

        #endregion


        private async Task<UserProfile> StoreInUserTable(IBotContext context)
        {
            UserTable userTable = new UserTable();
            return await userTable.AddUser(
                context.Activity.ChannelId,
                context.Activity.From.Id,
                context.UserData.GetValue<string>(NameKey),
                context.UserData.GetValue<string>(PhoneKey),
                context.UserData.GetValue<string>(EmailKey));
        }
    }
}