using System.Text;
using System.Text.Json;

using Common;

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BandAgent
{
    internal class VirtualAgent
    {
        private readonly ILogger<VirtualAgent> _logger;
        private readonly IOptions<EventHubOptions> _options;
        private static DeviceClient s_device;
        private TwinCollection _reportedProperties;

        public VirtualAgent(ILogger<VirtualAgent> logger, IOptions<EventHubOptions> options)
        {
            _logger = logger;
            _options = options;
            s_device = DeviceClient.CreateFromConnectionString(_options.Value.DeviceConnectionString);
        }

        internal async Task StartAsync(CancellationToken token)
        {
            _logger.LogInformation("Initializing Agent");
            Console.WriteLine("Initializing Agent");


            await s_device.OpenAsync(token);
            _ = ReceiveEventsAsync(s_device, token);

            // Showcase remote method invocation
            await s_device.SetMethodDefaultHandlerAsync(OtherDeviceMethod, null, token);
            await s_device.SetMethodHandlerAsync("ShowMessage", ShowMessage, null, token);

            _logger.LogInformation("Device Connected");

            await UpdateTwin(s_device);
            await s_device.SetDesiredPropertyUpdateCallbackAsync(UpdateProperties, null, token);

            Console.WriteLine("Press a key to perform an action:");
            Console.WriteLine("q: quits");
            Console.WriteLine("h: send happy feedback");
            Console.WriteLine("u: send unhappy feedback");
            Console.WriteLine("e: request emergency help");

            Random random = new();
            bool quitRequested = false;
            while (!quitRequested)
            {
                Console.WriteLine("Action? ");
                char input = Console.ReadKey().KeyChar;
                Console.WriteLine();
                StatusType status = StatusType.NotSpesified;
                int latitude = random.Next(0, 100);
                int longitude = random.Next(0, 100);

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
                Telemetry telemetry = new()
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Status = status,
                };

                string payload = JsonSerializer.Serialize(telemetry);
                Message message = new(Encoding.ASCII.GetBytes(payload));
                await s_device.SendEventAsync(message, token);

                _logger.LogInformation("Message sent!");
            }

        }

        private async Task UpdateProperties(TwinCollection desiredProperties, object userContext)
        {
            string currentFirmwareVersion = (string)_reportedProperties["firmwareVersion"];
            string desiredFirmawareVersion = (string)desiredProperties["firmwareVersion"];
            if (currentFirmwareVersion != desiredFirmawareVersion)
            {
                _logger.LogInformation("Firmware update requested. Current version {CurrentFirmwareVersion}" +
                                       "requested version: {DesiredFirmawareVersion}", currentFirmwareVersion, desiredFirmawareVersion);
                await ApplyFirmwareUpdateAsync(desiredFirmawareVersion);
            }
        }

        private async Task ApplyFirmwareUpdateAsync(string targetVersion)
        {
            _logger.LogInformation("Beginning firmware update...");
            _reportedProperties["firmwareUpdateStatus"] =
                $"Downloading zip file for the firmware {targetVersion}...";
            await s_device.UpdateReportedPropertiesAsync(_reportedProperties);
            Thread.Sleep(5000);

            _reportedProperties["firmwareUpdateStatus"] =
                $"Unzipping the package...";
            await s_device.UpdateReportedPropertiesAsync(_reportedProperties);
            Thread.Sleep(5000);

            _logger.LogInformation("Beginning firmware update...");
            _reportedProperties["firmwareUpdateStatus"] =
                $"Applying the update...";
            await s_device.UpdateReportedPropertiesAsync(_reportedProperties);
            Thread.Sleep(5000);

            _logger.LogInformation("Firmware update completed!");

            _reportedProperties["firmwareUpdateStatus"] = "n/a";
            _reportedProperties["firmwareVersion"] = targetVersion;
            await s_device.UpdateReportedPropertiesAsync(_reportedProperties);


        }

        private Task<MethodResponse> OtherDeviceMethod(
            MethodRequest methodRequest
            , object userContext)
        {
            _logger.LogInformation("***Other Device Method Called***");
            _logger.LogInformation("Method: {Name}", methodRequest.Name);
            _logger.LogInformation("Payload: {Payload}", methodRequest.DataAsJson);

            byte[] responsePayload = Encoding.ASCII.GetBytes(@"{""response"": ""method is not available"" }");

            return Task.FromResult(new MethodResponse(responsePayload, 404));
        }

        private static async Task ReceiveEventsAsync(DeviceClient device, CancellationToken cancellationToken)
        {
            while (true)
            {
                Message message = await device.ReceiveAsync(cancellationToken);
                if (message == null)
                {
                    continue;
                }
                byte[] messageBody = message.GetBytes();
                string payload = Encoding.ASCII.GetString(messageBody);
                Console.WriteLine($"Received message from cloud: '{payload}'");
                await device.CompleteAsync(message, cancellationToken);
            }
        }

        private async Task UpdateTwin(DeviceClient device)
        {
            _reportedProperties = new TwinCollection();
            _reportedProperties["firmwareVersion"] = "1.0";
            _reportedProperties["firmwareUpdateStatus"] = "n/a";

            await device.UpdateReportedPropertiesAsync(_reportedProperties);
        }

        private Task<MethodResponse> ShowMessage(
            MethodRequest methodRequest
            , object userContext)
        {
            _logger.LogInformation("***MESSAGE RECEIVED***");
            Console.WriteLine(methodRequest.DataAsJson);

            byte[] responsePayload = Encoding.ASCII.GetBytes(@"{""response"": ""Message Shown"" }");

            return Task.FromResult(new MethodResponse(responsePayload, 200));
        }
    }
}
