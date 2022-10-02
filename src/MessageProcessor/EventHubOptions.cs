using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageProcessor
{
    internal class EventHubOptions
    {
            public const string EventHub = "EventHub";

            public string HubName { get; set; } = String.Empty;
            public string IotHubConnectionString { get; set; } = String.Empty;
            public string StorageConnectionString { get; set; } = String.Empty;
            public string StorageContainerName { get; set; } = String.Empty;
    }
}
