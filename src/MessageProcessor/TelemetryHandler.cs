using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure;

using Common;

using MediatR;

namespace MessageProcessor
{
    internal class TelemetryHandler : IRequestHandler<Telemetry, Unit>
    {
        public string OperationId { get; }

        public TelemetryHandler()
        {
            OperationId = Guid.NewGuid().ToString()[^4..];
        }
        public Task<Unit> Handle(Telemetry request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Handler Constructor Generated id:{OperationId}");
            return Task.FromResult(new Unit());
        }
    }
}
