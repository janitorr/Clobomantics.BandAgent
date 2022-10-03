using Azure;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;
using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Azure;
using System.Runtime.CompilerServices;

namespace MessageProcessor
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices((context, services) =>
            {
                var configRoot = context.Configuration;
                services.AddSingleton<Processor>();
                services.Configure<EventHubOptions>(options =>
                    configRoot.GetSection(EventHubOptions.EventHub).Bind(options)
                );
            });

            builder.UseConsoleLifetime();
            var app = builder.Build();

            CancellationTokenSource cts = new();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Shutting down.");
                cts.Cancel();
                e.Cancel = true;
            };

            var messageProcessor = app.Services.GetRequiredService<Processor>();
            await messageProcessor.StartAsync(cts.Token);

            await app.WaitForShutdownAsync();
            
        }
    }
}