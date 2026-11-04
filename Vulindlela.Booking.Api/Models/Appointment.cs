namespace Vulindlela.Booking.Api.Models
{
    public enum AppointmentStatus
    {
        Active = 0,
        Cancelled = 1
    }

    public class Appointment
    {
        public int Id { get; set; }
        public int SlotId { get; set; }
        public string CitizenId { get; set; } = "";
        public string ServiceType { get; set; } = "";
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Active;
        public DateTime? ClearanceNotifiedAt { get; set; }
    }
}
