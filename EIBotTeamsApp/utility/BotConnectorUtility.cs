using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service.utility
{
    public static class BotConnectorUtility
    {
        public static async Task<ConnectorClient> BuildConnectorClientAsync(string serviceUrl)
        {
            var account = new MicrosoftAppCredentials(
                ConfigurationManager.AppSettings["MicrosoftAppId"],
                ConfigurationManager.AppSettings["MicrosoftAppPassword"]);
            Trace.TraceInformation($"MicrosoftAppId is {ConfigurationManager.AppSettings["MicrosoftAppId"]} and " +
                                   $"MicrosoftAppPassword is {ConfigurationManager.AppSettings["MicrosoftAppPassword"]}");
            var jwtToken = await account.GetTokenAsync();
            return new ConnectorClient(new Uri(serviceUrl), ConfigurationManager.AppSettings["MicrosoftAppId"],
                ConfigurationManager.AppSettings["MicrosoftAppPassword"], handlers: new AddAuthorizationHeaderHandler(jwtToken));
        }

        private class AddAuthorizationHeaderHandler : DelegatingHandler
        {
            private string _token;
            public AddAuthorizationHeaderHandler(string token)
            {
                _token = token;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                try
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                    var response = base.SendAsync(request, cancellationToken);
                    WebApiConfig.TelemetryClient.TrackEvent("BotConnectorUtility.AddAuthorizationHeaderHandler");
                    return response;
                }
                catch (Exception e)
                {
                    WebApiConfig.TelemetryClient.TrackException(e, new Dictionary<string, string>
                    {
                        {"class", "AddAuthorizationHeaderHandler" }
                    });
                    throw;
                }
            }
        }

        public static RetryPolicy BuildRetryPolicy()
        {
            // Define the Retry Strategy
            var retryStrategy = new ExponentialBackoff(3, TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(1));

            return new RetryPolicy(new WebExceptionDetectionStrategy(), retryStrategy);
        }

        public class WebExceptionDetectionStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex) => ex is WebException;
        }
    }
}