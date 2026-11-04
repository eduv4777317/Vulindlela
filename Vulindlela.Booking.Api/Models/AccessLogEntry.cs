namespace Vulindlela.Booking.Api.Models
{
    public class AccessLogEntry
    {
        public int Id { get; set; }
        public string CitizenId { get; set; } = "";
        public string AccessedBy { get; set; } = "";
        public string AccessedByRole { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime AccessedAt { get; set; }
    }
}
