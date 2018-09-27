using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System.Web.Http;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Office.EIBot.Service.utility;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        BotJwtRefreshWorker _botJwtRefreshWorker;

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

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

            var tableStore = new TableBotDataStore(System.Configuration.ConfigurationManager
                .ConnectionStrings["StorageConnectionString"]
                .ConnectionString);

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
                });
#endif

            _botJwtRefreshWorker = new BotJwtRefreshWorker();
        }

        protected void Application_End()
        {
            _botJwtRefreshWorker.Dispose();
            _botJwtRefreshWorker = null;
        }
    }
}
