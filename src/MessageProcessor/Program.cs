using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessageProcessor
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            IHostBuilder builder = Host.CreateDefaultBuilder(args);
            _ = builder.ConfigureServices((context, services) =>
            {
                IConfiguration configRoot = context.Configuration;
                _ = services.AddSingleton<Processor>();
                _ = services.Configure<EventHubOptions>(options =>
                    configRoot.GetSection(EventHubOptions.EventHub).Bind(options)
                );
            });

            _ = builder.UseConsoleLifetime();
            IHost app = builder.Build();

            CancellationTokenSource cts = new();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Shutting down.");
                cts.Cancel();
                e.Cancel = true;
            };

            Processor messageProcessor = app.Services.GetRequiredService<Processor>();
            await messageProcessor.StartAsync(cts.Token);

            await app.WaitForShutdownAsync();

        }
    }
}