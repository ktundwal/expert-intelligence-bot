using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service.utility
{
    sealed class BotJwtRefreshWorker : IDisposable
    {
        CancellationTokenSource _Cts = new CancellationTokenSource();

        public BotJwtRefreshWorker()
        {
            var appID = System.Configuration.ConfigurationManager.AppSettings["MicrosoftAppId"];
            var appPassword = System.Configuration.ConfigurationManager.AppSettings["MicrosoftAppPassword"];
            if (!string.IsNullOrEmpty(appID) && !string.IsNullOrEmpty(appPassword))
            {
                var credentials = new MicrosoftAppCredentials(appID, appPassword);
                Task.Factory.StartNew(
                    async () =>
                    {
                        var ct = _Cts.Token;
                        while (!ct.IsCancellationRequested)
                        {
                            try
                            {
                                // GetTokenAsync method internally calls RefreshAndStoreToken,
                                // meaning that the token will automatically be cached at this point
                                // and you don’t need to do anything else – the bot will always have a valid token.
                                await credentials.GetTokenAsync().ConfigureAwait(false);
                                WebApiConfig.TelemetryClient.TrackEvent("BotJwtRefreshWorker.TokenRefreshed");
                            }
                            catch (Exception ex)
                            {
                                WebApiConfig.TelemetryClient.TrackException(ex, new Dictionary<string, string>
                                {
                                    {"class", "BotJwtRefreshWorker" }
                                });
                            }
                            await Task.Delay(TimeSpan.FromMinutes(30), ct).ConfigureAwait(false);
                        }
                    },
                    TaskCreationOptions.LongRunning);
            }
        }

        public void Dispose()
        {
            _Cts.Cancel();
            _Cts.Dispose();
        }
    }
}