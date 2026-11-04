namespace Vulindlela.Clearance.Api.Models
{
    public class AppointmentRecord
    {
        public int AppointmentId { get; set; }
        public string CitizenId { get; set; } = "";
        public string ServiceType { get; set; } = "";
        public DateTime StartsAt { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
