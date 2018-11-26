using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Teams.TemplateBotCSharp;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Microsoft.Office.EIBot.Service.utility
{
    public static class SendGridMailHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from">var from = new EmailAddress("eibotagents@microsoft.com", "Expert Connect Bot");</param>
        /// <param name="to">var to = new EmailAddress("katundwa@microsoft.com", "Kapil Tundwal");</param>
        /// <param name="subject"></param>
        /// <param name="plainTextContent"></param>
        /// <param name="htmlContent"></param>
        /// <returns></returns>
        public static async Task SendEmail(EmailAddress from,
            EmailAddress to,
            string vsoId,
            string subject,
            string plainTextContent,
            string htmlContent)
        {
            var properties = new Dictionary<string, string>
            {
                {"function", "SendEmail" },
                {"from", from.Email },
                {"to", to.Email },
                {"plainTextContent", plainTextContent },
                {"vsoId", vsoId }
            };

            try
            {
                var apiKey = ConfigurationManager.AppSettings["sendgridapikey"];
                var client = new SendGridClient(apiKey);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg);

                properties.Add("sendGridApiStatusCode", response.StatusCode.ToString());
                WebApiConfig.TelemetryClient.TrackEvent("SendEmail", properties);
            }
            catch (Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);
            }
        }
    }
}