using Microsoft.Extensions.Logging.Abstractions;
using Vulindlela.Booking.Api.Clients;
using Vulindlela.Booking.Api.Models;
using Vulindlela.Booking.Api.Services;

namespace Vulindlela.Tests.Booking
{
    public class ClearanceFailureTests
    {
        [Fact]
        public async Task Booking_IsSentToClearance_AndMarkedAsNotified()
        {
            using var database = new TestDatabase();
            var slotId = await database.AddSlotAsync(DateTime.UtcNow.AddDays(1));
            var notifier = new FakeClearanceNotifier(succeeds: true);
            using var db = database.CreateContext();

            var result = await TestDatabase.CreateService(db, notifier)
                .BookAppointmentAsync(slotId, TestCitizens.Thandi, ServiceTypes.Passport);

            Assert.Equal(BookingOutcome.Booked, result.Outcome);
            Assert.Equal(new[] { result.Appointment!.Id }, notifier.NotifiedAppointmentIds);
            Assert.NotNull((await db.Appointments.FindAsync(result.Appointment.Id))!.ClearanceNotifiedAt);
        }

        [Fact]
        public async Task Booking_StillSucceeds_WhenClearanceCannotBeReached()
        {
            using var database = new TestDatabase();
            var slotId = await database.AddSlotAsync(DateTime.UtcNow.AddDays(1));
            using var db = database.CreateContext();

            var result = await TestDatabase.CreateService(db, new FakeClearanceNotifier(succeeds: false))
                .BookAppointmentAsync(slotId, TestCitizens.Thandi, ServiceTypes.Passport);

            Assert.Equal(BookingOutcome.Booked, result.Outcome);
            Assert.Equal(1, await database.CountActiveAppointmentsAsync(slotId));
            Assert.Null((await db.Appointments.FindAsync(result.Appointment!.Id))!.ClearanceNotifiedAt);
        }

        [Fact]
        public async Task Notifier_ReturnsFalse_InsteadOfThrowing_WhenClearanceIsStopped()
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://127.0.0.1:1"),
                Timeout = TimeSpan.FromSeconds(2)
            };
            var notifier = new ClearanceNotifier(httpClient, NullLogger<ClearanceNotifier>.Instance);

            var sent = await notifier.NotifyAppointmentCreatedAsync(
                new Appointment { Id = 1, CitizenId = TestCitizens.Thandi, ServiceType = ServiceTypes.Passport },
                new Slot { Id = 1, BranchId = 1, StartsAt = DateTime.UtcNow.AddDays(1) });

            Assert.False(sent);
        }
    }
}
