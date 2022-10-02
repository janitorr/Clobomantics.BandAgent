using Common;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BandAgent
{
    internal class VirtualAgent
    {
        private readonly ILogger<VirtualAgent> _logger;
        private readonly IOptions<EventHubOptions> _options;

        public VirtualAgent(ILogger<VirtualAgent> logger, IOptions<EventHubOptions> options)
        {
            _logger = logger;
            _options = options;
        }

        internal async Task ExecuteAsync(CancellationToken token)
        {
            _logger.LogInformation("Initializing Agent");
            Console.WriteLine("Initializing Agent");

            var device = DeviceClient.CreateFromConnectionString(_options.Value.DeviceConnectionString);
            await device.OpenAsync(token);
            _ = ReceiveEventsAsync(device, token);

            // Showcase remote method invocation
            await device.SetMethodDefaultHandlerAsync(OtherDeviceMethod, null, token);
            await device.SetMethodHandlerAsync("ShowMessage", ShowMessage, null, token);

            _logger.LogInformation("Device Connected");

            await UpdateTwin(device);

            Console.WriteLine("Press a key to perform an action:");
            Console.WriteLine("q: quits");
            Console.WriteLine("h: send happy feedback");
            Console.WriteLine("u: send unhappy feedback");
            Console.WriteLine("e: request emergency help");

            var random = new Random();
            var quitRequested = false;
            while (!quitRequested)
            {
                Console.WriteLine("Action? ");
                var input = Console.ReadKey().KeyChar;
                Console.WriteLine();
                var status = StatusType.NotSpesified;
                var latitude = random.Next(0, 100);
                var longitude = random.Next(0, 100);

                switch (input)
                {
                    case 'q':
                        quitRequested = true;
                        break;
                    case 'h':
                        status = StatusType.Happy;
                        break;
                    case 'u':
                        status = StatusType.Unhappy;
                        break;

                    case 'e':
                        status = StatusType.Emergency;
                        break;
                    default:
                        status = StatusType.NotSpesified;
                        break;
                }
                var telemetry = new Common.Telemetry
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Status = status,
                };

                var payload = JsonSerializer.Serialize(telemetry);
                var message = new Message(Encoding.ASCII.GetBytes(payload));
                await device.SendEventAsync(message, token);

                _logger.LogInformation("Message sent!");
            }

        }

        private Task<MethodResponse> OtherDeviceMethod(
            MethodRequest methodRequest
            , object userContext)
        {
            _logger.LogInformation("***Other Device Method Called***");
            _logger.LogInformation("Method: {Name}", methodRequest.Name);
            _logger.LogInformation("Payload: {Payload}", methodRequest.DataAsJson);

            var responsePayload = Encoding.ASCII.GetBytes(@"{""response"": ""method is not available"" }");

            return Task.FromResult(new MethodResponse(responsePayload, 404));
        }

        private static async Task ReceiveEventsAsync(DeviceClient device, CancellationToken cancellationToken)
        {
            while (true)
            {
                var message = await device.ReceiveAsync(cancellationToken);
                if (message == null)
                {
                    continue;
                }
                var messageBody = message.GetBytes();
                var payload = Encoding.ASCII.GetString(messageBody);
                Console.WriteLine($"Received message from cloud: '{payload}'");
                await device.CompleteAsync(message, cancellationToken);
            }
        }

        private static async Task UpdateTwin(DeviceClient device)
        {
            var twinPropertios = new TwinCollection();
            twinPropertios["connectionType"] = "wi-fi";
            twinPropertios["connectionStrength"] = "weak";

            await device.UpdateReportedPropertiesAsync(twinPropertios);
        }

        private Task<MethodResponse> ShowMessage(
            MethodRequest methodRequest
            , object userContext)
        {
            _logger.LogInformation("***MESSAGE RECEIVED***");
            Console.WriteLine(methodRequest.DataAsJson);

            var responsePayload = Encoding.ASCII.GetBytes(@"{""response"": ""Message Shown"" }");

            return Task.FromResult(new MethodResponse(responsePayload, 200));
        }
    }
}
