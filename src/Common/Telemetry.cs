using MediatR;

namespace Common
{
    public class Telemetry : IRequest<Unit>
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public StatusType Status { get; set; }
    }
}
