using System.Text;

using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BandManager
{
    internal class DeviceManager
    {
        private readonly ILogger<DeviceManager> _logger;
        private readonly IOptions<EventHubOptions> _options;

        public DeviceManager(ILogger<DeviceManager> logger, IOptions<EventHubOptions> options)
        {
            _logger = logger;
            _options = options;
        }

        internal async Task StartAsync(CancellationToken token)
        {
            var serviceClient = ServiceClient.CreateFromConnectionString(_options.Value.ServiceKey);

            var feedbackTask = ReceiveFeedback(serviceClient, token);

            while (true)
            {
                Console.WriteLine("Which device do you wish to send a message to?");
                Console.Write("> ");
                //var deviceId = Console.ReadLine();
                //Lazy bastard                
                var deviceId = "device-01";

                /* CLOUD TO DEVICE MESSAGE */
                await SendCloudToDeviceMessage(serviceClient, deviceId);

                /* DIRECT METHOD CALL */
                await CallDirectMethod(serviceClient, deviceId);

                _logger.LogTrace("Message sent to device");
            }
        }

        private async Task CallDirectMethod(
            ServiceClient serviceClient
            , string deviceId)
        {
            var method = new CloudToDeviceMethod("ShowMessage");
            method.SetPayloadJson("'Hello from C#'");

            var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, method);

            _logger.LogInformation("Response status: {Status}, payload {PayLoad}", response.Status, response.GetPayloadAsJson());
        }

        private async Task ReceiveFeedback(ServiceClient serviceClient, CancellationToken cancellationToken)
        {
            var feedbackReceiver = serviceClient.GetFeedbackReceiver();

            while (true)
            {
                FeedbackBatch feedbackBatch = await feedbackReceiver.ReceiveAsync(cancellationToken);
                if (feedbackBatch == null)
                {
                    continue;
                }

                foreach (var record in feedbackBatch.Records)
                {
                    var messageId = record.OriginalMessageId;
                    var status = record.StatusCode;

                    _logger.LogInformation("Feedback received for message {MessageId}, status code: {Status}", messageId, status);
                }
                await feedbackReceiver.CompleteAsync(feedbackBatch, cancellationToken);
            }
        }

        private static async Task SendCloudToDeviceMessage(ServiceClient serviceClient, string? deviceId)
        {
            Console.WriteLine("What message payload do you want to send?");
            Console.Write("> ");

            var payload = Console.ReadLine() ?? "No message";


            var commandMessage = new Message(Encoding.ASCII.GetBytes(payload))
            {
                MessageId = Guid.NewGuid().ToString(),
                Ack = DeliveryAcknowledgement.Full,
                ExpiryTimeUtc = DateTime.UtcNow.AddSeconds(10)
            };

            await serviceClient.SendAsync(deviceId, commandMessage);
        }
    }
}
