using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Amqp;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace MessageProcessor
{
    internal class Processor
    {
        private readonly ILogger<Processor> _logger;
        private readonly IOptions<EventHubOptions> _options;

        public Processor(ILogger<Processor> logger
                        ,IOptions<EventHubOptions> options)
        {
            _logger = logger;
            _options = options;
        }

        internal async Task StartAsync(CancellationToken cancellationToken)
        {
            var consuremGroupName = EventHubConsumerClient.DefaultConsumerGroupName;

            var blobClient = new BlobContainerClient(
                _options.Value.StorageConnectionString, 
                _options.Value.StorageContainerName);

            var processor = new EventProcessorClient(
                blobClient
                , consuremGroupName
                , _options.Value.IotHubConnectionString
                , _options.Value.HubName);

            processor.ProcessEventAsync += Processor_ProcessEventAsync;
            processor.ProcessErrorAsync += Processor_ProcessErrorAsync;
            await processor.StartProcessingAsync(cancellationToken);

            Console.WriteLine("Event processor started, press anything to exit.");
            Console.ReadKey();

            await processor.StopProcessingAsync(cancellationToken);
        }
    

        private static Task Processor_ProcessErrorAsync(Azure.Messaging.EventHubs.Processor.ProcessErrorEventArgs arg)
        {
            throw new NotImplementedException();
        }

        private  Task Processor_ProcessEventAsync(Azure.Messaging.EventHubs.Processor.ProcessEventArgs arg)
        {
            Console.WriteLine($"processing from partition: {arg.Partition.PartitionId}");
            var paylaod = arg.Data.EventBody.ToObjectFromJson<Telemetry>();
            var deviceId = arg.Data.SystemProperties["iothub-connection-device-id"];
            Console.WriteLine($"Message received on partition '{arg.Partition.PartitionId}'," +
                              $"device ID: {deviceId}," +
                              $"payload: {paylaod}");

            if (paylaod.Status == StatusType.Emergency)
            {
                _logger.LogWarning("Alarm alarm!");
            }
            return arg.UpdateCheckpointAsync();
        }
    }
}
