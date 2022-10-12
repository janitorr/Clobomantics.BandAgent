using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;

using Common;

using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MessageProcessor
{
    internal class Processor
    {
        private readonly ILogger<Processor> _logger;
        private readonly IOptions<EventHubOptions> _options;
        private readonly ISender _sender;
        private readonly ITelemetryService _telemetryService;

        public Processor(ILogger<Processor> logger
                        , IOptions<EventHubOptions> options
                        , ISender sender
                        , ITelemetryService telemetryService)
        {
            _logger = logger;
            _options = options;
            _sender = sender;
            _telemetryService = telemetryService;
        }

        internal async Task StartAsync(CancellationToken cancellationToken)
        {
            string consuremGroupName = EventHubConsumerClient.DefaultConsumerGroupName;

            BlobContainerClient blobClient = new(
                _options.Value.StorageConnectionString,
                _options.Value.StorageContainerName);

            EventProcessorClient processor = new(
                blobClient
                , consuremGroupName
                , _options.Value.IotHubConnectionString
                , _options.Value.HubName);

            processor.ProcessEventAsync += Processor_ProcessEventAsync;
            processor.ProcessErrorAsync += Processor_ProcessErrorAsync;
            await processor.StartProcessingAsync(cancellationToken);

            Console.WriteLine("Event processor started, press anything to exit.");
            _ = Console.ReadKey();

            await processor.StopProcessingAsync(cancellationToken);
        }


        private static Task Processor_ProcessErrorAsync(Azure.Messaging.EventHubs.Processor.ProcessErrorEventArgs arg)
        {
            throw new NotImplementedException();
        }

        private Task Processor_ProcessEventAsync(Azure.Messaging.EventHubs.Processor.ProcessEventArgs arg)
        {
            Console.WriteLine($"processing from partition: {arg.Partition.PartitionId}");
            _logger.LogInformation("Type of message was {Type}", arg.Data.Properties["type"]);
            Telemetry paylaod = arg.Data.EventBody.ToObjectFromJson<Telemetry>();
            object deviceId = arg.Data.SystemProperties["iothub-connection-device-id"];
            Console.WriteLine($"Message received on partition '{arg.Partition.PartitionId}'," +
                              $"device ID: {deviceId}," +
                              $"payload: {paylaod}");
            _sender.Send(paylaod, arg.CancellationToken);
            _telemetryService.Handle(paylaod, arg.CancellationToken);
            if (paylaod.Status == StatusType.Emergency)
            {
                _logger.LogWarning("Alarm alarm!");
            }
            return arg.UpdateCheckpointAsync();
        }
    }
}
