using System.ComponentModel.DataAnnotations;

namespace Vulindlela.Booking.Api.Contracts
{
    public sealed class BookAppointmentRequest
    {
        [Required]
        public int? SlotId { get; init; }

        [Required]
        public string? CitizenId { get; init; }

        [Required]
        public string? ServiceType { get; init; }
    }
}
