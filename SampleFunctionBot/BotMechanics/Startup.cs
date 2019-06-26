using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SampleFunctionBot.BotMechanics.Dialogs;
using SampleFunctionBot.GamePlay;
using System;
using System.IO;

[assembly: FunctionsStartup(typeof(SampleFunctionBot.Startup))]
namespace SampleFunctionBot
{

    /// <summary>
    /// Let's wire up our Dependencies so they can be injected by the runtime for us.
    /// </summary>

    internal class Startup : FunctionsStartup
    {
        public IBotFrameworkHttpAdapter BindToAdapter(IServiceProvider e) {

            var ae = new BotFrameworkHttpAdapter(e.GetService<ICredentialProvider>());
            ae.Use(new ShowTypingMiddleware(500, 2000));
           
            return (IBotFrameworkHttpAdapter) ae;
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {

            builder.Services.AddSingleton<ICredentialProvider>(e => new SimpleCredentialProvider(
                Environment.GetEnvironmentVariable("MicrosoftAppId"),
                Environment.GetEnvironmentVariable("MicrosoftAppPassword")
            ));

            builder.Services.AddSingleton<IBotFrameworkHttpAdapter>(e => BindToAdapter(e));

            builder.Services.AddSingleton<IStorage>(e => new MemoryStorage());

            // OR -- it's recommended  to use cosmosDB for the state store to make the bot stateless
            
            //builder.Services.AddSingleton<IStorage>(e => new CosmosDbStorage(
            //new CosmosDbStorageOptions()
            //{   CosmosDBEndpoint = new Uri(Environment.GetEnvironmentVariable("CosmosDBEndpoint")),
            //    AuthKey = Environment.GetEnvironmentVariable("CosmosAuthKey"),
            //    CollectionId = Environment.GetEnvironmentVariable("CosmosCollectionId"),
            //    DatabaseId = Environment.GetEnvironmentVariable("CosmosDatabaseId"),

            //}));

            builder.Services.AddSingleton<GameData>(e => JsonConvert.DeserializeObject<GameData>(File.ReadAllText(Environment.GetEnvironmentVariable("GameFile"))));

            builder.Services.AddTransient<ConversationState>();
            builder.Services.AddTransient<UserState>();

            builder.Services.AddTransient<Dialog>(e => new GameSetupDialog(
                e.GetService<GameData>()));
            
            builder.Services.AddTransient<IBot>(e => new GameBot(
                e.GetService<ConversationState>(), 
                e.GetService<GameData>(), 
                e.GetService<Dialog>())
                );

        } 
    }
}
