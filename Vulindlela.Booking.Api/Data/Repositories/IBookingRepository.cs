using Vulindlela.Booking.Api.Models;

namespace Vulindlela.Booking.Api.Data.Repositories
{
    public enum InsertAppointmentResult
    {
        Inserted,
        SlotAlreadyTaken
    }

    public interface IBookingRepository
    {
        Task<IReadOnlyList<Branch>> GetBranchesAsync();
        Task<Branch?> GetBranchByIdAsync(int id);

        Task<Slot?> GetSlotByIdAsync(int id);
        Task<IReadOnlyList<Slot>> GetSlotsForBranchAsync(int branchId, DateTime fromUtc, DateTime toUtc);
        Task<IReadOnlyList<Slot>> GetSlotsByIdsAsync(IReadOnlyCollection<int> slotIds);

        Task<Appointment?> GetAppointmentByIdAsync(int id);
        Task<IReadOnlyList<Appointment>> GetAppointmentsForSlotAsync(int slotId);
        Task<IReadOnlyList<Appointment>> GetAppointmentsForSlotsAsync(IReadOnlyCollection<int> slotIds);
        Task<IReadOnlyList<Appointment>> GetAppointmentsForCitizenAsync(string citizenId);

        Task<InsertAppointmentResult> InsertAppointmentAsync(Appointment appointment);

        Task AddAccessLogEntryAsync(AccessLogEntry entry);
        Task<IReadOnlyList<AccessLogEntry>> GetAccessLogAsync(string citizenId);

        Task SaveChangesAsync();
    }
}
