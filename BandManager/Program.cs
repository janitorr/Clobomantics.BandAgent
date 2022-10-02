using Microsoft.Azure.Devices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;

namespace BandManager
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var app = Host.CreateDefaultBuilder(args)
                    .ConfigureServices((context, services) =>
                    { 
                        services.AddSingleton<DeviceManager>();

                    })
                    .Build();

            CancellationTokenSource cts = new();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Shutting down.");
                cts.Cancel();
                e.Cancel = true;
            };

            var main = app.Services.GetRequiredService<DeviceManager>();
            await main.ExecuteAsync(cts.Token);
        }
    }
}