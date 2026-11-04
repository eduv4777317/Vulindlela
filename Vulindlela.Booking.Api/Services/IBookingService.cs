using Vulindlela.Booking.Api.Models;

namespace Vulindlela.Booking.Api.Services
{
    public interface IBookingService
    {
        Task<IReadOnlyList<Branch>> GetBranchesAsync();

        Task<IReadOnlyList<Slot>?> GetAvailableSlotsAsync(int branchId, DateOnly? fromDate, int days);

        Task<BookingResult> BookAppointmentAsync(int slotId, string citizenId, string serviceType);

        Task<Appointment?> GetAppointmentAsync(int id);

        Task<IReadOnlyList<Appointment>> GetAppointmentsForCitizenAsync(string citizenId);

        Task<IReadOnlyList<Slot>> GetSlotsAsync(IReadOnlyCollection<int> slotIds);

        Task<CancelOutcome> CancelAppointmentAsync(int id);
    }
}
