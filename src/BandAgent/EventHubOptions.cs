using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BandAgent
{
    internal class EventHubOptions
    {
        public const string EventHub = "EventHub";

        public string DeviceConnectionString { get; set; } = string.Empty;
    }
}
