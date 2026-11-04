using Vulindlela.Booking.Api.Models;

namespace Vulindlela.Booking.Api.Clients
{
    public interface IClearanceNotifier
    {
        Task<bool> NotifyAppointmentCreatedAsync(Appointment appointment, Slot slot);
    }
}
