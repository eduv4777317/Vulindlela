using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Models;

namespace Vulindlela.Booking.Api.Data.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private const int SqliteConstraint = 19;
        private const int SqliteConstraintUnique = 2067;

        private readonly BookingDBContext _bookingDBContext;

        public BookingRepository(BookingDBContext bookingDBContext)
        {
            _bookingDBContext = bookingDBContext;
        }

        public async Task<IReadOnlyList<Branch>> GetBranchesAsync()
        {
            return await _bookingDBContext.Branches
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<Branch?> GetBranchByIdAsync(int id)
        {
            return await _bookingDBContext.Branches.FindAsync(id);
        }

        public async Task<Slot?> GetSlotByIdAsync(int id)
        {
            return await _bookingDBContext.Slots.FindAsync(id);
        }

        public async Task<IReadOnlyList<Slot>> GetSlotsForBranchAsync(int branchId, DateTime fromUtc, DateTime toUtc)
        {
            return await _bookingDBContext.Slots
                .AsNoTracking()
                .Where(s => s.BranchId == branchId && s.StartsAt >= fromUtc && s.StartsAt < toUtc)
                .OrderBy(s => s.StartsAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Slot>> GetSlotsByIdsAsync(IReadOnlyCollection<int> slotIds)
        {
            return await _bookingDBContext.Slots
                .AsNoTracking()
                .Where(s => slotIds.Contains(s.Id))
                .ToListAsync();
        }

        public async Task<Appointment?> GetAppointmentByIdAsync(int id)
        {
            return await _bookingDBContext.Appointments.FindAsync(id);
        }

        public async Task<IReadOnlyList<Appointment>> GetAppointmentsForSlotAsync(int slotId)
        {
            return await _bookingDBContext.Appointments
                .AsNoTracking()
                .Where(a => a.SlotId == slotId)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Appointment>> GetAppointmentsForSlotsAsync(IReadOnlyCollection<int> slotIds)
        {
            return await _bookingDBContext.Appointments
                .AsNoTracking()
                .Where(a => slotIds.Contains(a.SlotId))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Appointment>> GetAppointmentsForCitizenAsync(string citizenId)
        {
            return await _bookingDBContext.Appointments
                .AsNoTracking()
                .Where(a => a.CitizenId == citizenId)
                .OrderBy(a => a.Id)
                .ToListAsync();
        }

        public async Task<InsertAppointmentResult> InsertAppointmentAsync(Appointment appointment)
        {
            _bookingDBContext.Appointments.Add(appointment);

            try
            {
                await _bookingDBContext.SaveChangesAsync();
                return InsertAppointmentResult.Inserted;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _bookingDBContext.Entry(appointment).State = EntityState.Detached;
                return InsertAppointmentResult.SlotAlreadyTaken;
            }
        }

        public async Task SaveChangesAsync()
        {
            await _bookingDBContext.SaveChangesAsync();
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is SqliteException
            {
                SqliteErrorCode: SqliteConstraint,
                SqliteExtendedErrorCode: SqliteConstraintUnique
            };
    }
}
