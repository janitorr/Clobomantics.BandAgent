using BandAgent;
using Common;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.Json;
namespace Clobomantics.BandAgent
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices((context, services) =>
            {
                var configRoot = context.Configuration;
                services.AddSingleton<VirtualAgent>();
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

            var virtualAgent = app.Services.GetRequiredService<VirtualAgent>();
            await virtualAgent.StartAsync(cts.Token);
            await app.WaitForShutdownAsync(cts.Token);
        }
    }
}