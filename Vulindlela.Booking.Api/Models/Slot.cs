namespace Vulindlela.Booking.Api.Models
{
    public class Slot
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public DateTime StartsAt { get; set; }
    }
}
