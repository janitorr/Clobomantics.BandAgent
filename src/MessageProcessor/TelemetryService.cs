using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Common;
using MediatR;

namespace MessageProcessor
{
    internal class TelemetryService : ITelemetryService
    {
        public string OperationId { get; }

        public TelemetryService()
        {
            OperationId = Guid.NewGuid().ToString()[^4..];
        }

        public Task Handle(Telemetry request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Service Constructor Generated id:{OperationId}");
            return Task.CompletedTask;
        }
    }

    internal interface ITelemetryService
    {
        Task Handle(Telemetry request, CancellationToken cancellationToken);
    }
}
