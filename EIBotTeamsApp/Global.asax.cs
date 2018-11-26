using System;
using System.Configuration;
using System.Diagnostics;
using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System.Web.Http;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;
using Microsoft.WindowsAzure.Storage;
using TableLoggerModule = Microsoft.Office.EIBot.Service.utility.TableLoggerModule;

namespace Microsoft.Office.EIBot.Service
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        BotJwtRefreshWorker _botJwtRefreshWorker;

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            VerifyConfigurationIsValid();

            /*
            // Use an in-memory store for bot data.
            // This registers a IBotDataStore singleton that will be used throughout the app.
            var store = new InMemoryDataStore();

            Conversation.UpdateContainer(builder =>
            {
                builder.Register(c => new CachingBotDataStore(store,
                         CachingBotDataStoreConsistencyPolicy
                         .ETagBasedConsistency))
                         .As<IBotDataStore<BotData>>()
                         .AsSelf()
                         .InstancePerLifetimeScope();
            });
            */

            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];
            var tableStore = new TableBotDataStore(connectionString);

            var tableName = "ChatHistory";
            var account = CloudStorageAccount.Parse(connectionString);

            Conversation.UpdateContainer(
                builder =>
                {
                    builder.Register(c => tableStore)
                        .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                        .AsSelf()
                        .SingleInstance();

                    builder.Register(c => new CachingBotDataStore(tableStore,
                            CachingBotDataStoreConsistencyPolicy
                                .ETagBasedConsistency))
                        .As<IBotDataStore<BotData>>()
                        .AsSelf()
                        .InstancePerLifetimeScope();

                    builder.RegisterModule(new TableLoggerModule(account, tableName));
                });

            _botJwtRefreshWorker = new BotJwtRefreshWorker();
        }

        private void VerifyConfigurationIsValid()
        {
            string[] requiredConfigs =
            {
                "VsoOrgUrl",
                "MicrosoftAppId",
                "VsoUsername",
                "FancyHandsConsumerKey",
                "BotPhoneNumber",
                "AgentToAssignVsoTasksTo",
                "StorageConnectionString"
            };
            foreach (var requiredConfig in requiredConfigs)
            {
                var appSetting = ConfigurationManager.AppSettings[requiredConfig];
                Trace.TraceInformation($"AppSetting: {requiredConfig}: {appSetting}");
                if (string.IsNullOrEmpty(appSetting))
                {
                    throw new Exception($"{requiredConfig} not set. Please verify Azure AppSettings " +
                                        $"or eibot.secretAppSettings.config if running local ");
                }
            }
        }

        protected void Application_End()
        {
            _botJwtRefreshWorker.Dispose();
            _botJwtRefreshWorker = null;
        }
    }
}
