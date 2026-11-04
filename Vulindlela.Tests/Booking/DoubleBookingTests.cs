using Vulindlela.Booking.Api.Data.Repositories;
using Vulindlela.Booking.Api.Models;
using Vulindlela.Booking.Api.Services;

namespace Vulindlela.Tests.Booking
{
    public class DoubleBookingTests
    {
        [Theory]
        [InlineData(2)]
        [InlineData(20)]
        [InlineData(50)]
        public async Task ConcurrentBookingsForOneSlot_ExactlyOneSucceeds(int requests)
        {
            using var database = new TestDatabase();
            var slotId = await database.AddSlotAsync(DateTime.UtcNow.AddDays(1));

            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var bookings = Enumerable.Range(0, requests).Select(_ => Task.Run(async () =>
            {
                await gate.Task;
                using var db = database.CreateContext();
                return await TestDatabase.CreateService(db)
                    .BookAppointmentAsync(slotId, TestCitizens.Thandi, ServiceTypes.Passport);
            })).ToList();

            gate.SetResult();
            var results = await Task.WhenAll(bookings);

            Assert.Single(results, r => r.Outcome == BookingOutcome.Booked);
            Assert.All(results.Where(r => r.Outcome != BookingOutcome.Booked),
                r => Assert.Equal(BookingOutcome.SlotAlreadyTaken, r.Outcome));
            Assert.Equal(1, await database.CountActiveAppointmentsAsync(slotId));
        }

        [Fact]
        public async Task SecondActiveAppointmentForSameSlot_IsRefusedByTheDatabase()
        {
            using var database = new TestDatabase();
            var slotId = await database.AddSlotAsync(DateTime.UtcNow.AddDays(1));

            var first = await InsertDirectlyAsync(database, slotId, TestCitizens.Thandi);
            var second = await InsertDirectlyAsync(database, slotId, TestCitizens.Sipho);

            Assert.Equal(InsertAppointmentResult.Inserted, first);
            Assert.Equal(InsertAppointmentResult.SlotAlreadyTaken, second);
        }

        [Fact]
        public async Task CancelledAppointment_ReleasesTheSlot()
        {
            using var database = new TestDatabase();
            var slotId = await database.AddSlotAsync(DateTime.UtcNow.AddDays(1));
            using var db = database.CreateContext();
            var service = TestDatabase.CreateService(db);

            var first = await service.BookAppointmentAsync(slotId, TestCitizens.Thandi, ServiceTypes.Passport);
            var cancelled = await service.CancelAppointmentAsync(first.Appointment!.Id);
            var rebooked = await service.BookAppointmentAsync(slotId, TestCitizens.Sipho, ServiceTypes.Passport);

            Assert.Equal(CancelOutcome.Cancelled, cancelled);
            Assert.Equal(BookingOutcome.Booked, rebooked.Outcome);
        }

        private static async Task<InsertAppointmentResult> InsertDirectlyAsync(TestDatabase database, int slotId, string citizenId)
        {
            using var db = database.CreateContext();
            return await new BookingRepository(db).InsertAppointmentAsync(new Appointment
            {
                SlotId = slotId,
                CitizenId = citizenId,
                ServiceType = ServiceTypes.Passport
            });
        }
    }
}
