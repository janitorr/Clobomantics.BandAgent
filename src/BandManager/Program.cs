using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BandManager
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices((context, services) =>
            {
                var configRoot = context.Configuration;
                services.AddSingleton<DeviceManager>();
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

            var deviceManager = app.Services.GetRequiredService<DeviceManager>();
            await deviceManager.StartAsync(cts.Token);
            await app.WaitForShutdownAsync(cts.Token);
        }
    }
}