using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Clients;
using Vulindlela.Booking.Api.Data;
using Vulindlela.Booking.Api.Data.Repositories;
using Vulindlela.Booking.Api.Models;
using Vulindlela.Booking.Api.Services;

namespace Vulindlela.Tests.Booking
{
    public sealed class TestDatabase : IDisposable
    {
        private readonly string _connectionString =
            $"Data Source=test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        private readonly SqliteConnection _keepAlive;

        public TestDatabase()
        {
            _keepAlive = new SqliteConnection(_connectionString);
            _keepAlive.Open();

            using var db = CreateContext();
            db.Database.EnsureCreated();
        }

        public BookingDBContext CreateContext() =>
            new(new DbContextOptionsBuilder<BookingDBContext>().UseSqlite(_connectionString).Options);

        public static BookingService CreateService(BookingDBContext db, IClearanceNotifier? notifier = null) =>
            new(new BookingRepository(db), notifier ?? new FakeClearanceNotifier(succeeds: true));

        public async Task<int> AddSlotAsync(DateTime startsAtUtc)
        {
            using var db = CreateContext();
            var branch = new Branch { Name = "Test branch" };
            db.Branches.Add(branch);
            await db.SaveChangesAsync();

            var slot = new Slot { BranchId = branch.Id, StartsAt = startsAtUtc };
            db.Slots.Add(slot);
            await db.SaveChangesAsync();
            return slot.Id;
        }

        public async Task<int> CountActiveAppointmentsAsync(int slotId)
        {
            using var db = CreateContext();
            return await db.Appointments.CountAsync(a => a.SlotId == slotId && a.Status == AppointmentStatus.Active);
        }

        public void Dispose() => _keepAlive.Dispose();
    }
}
