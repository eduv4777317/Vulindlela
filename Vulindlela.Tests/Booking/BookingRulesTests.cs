using Vulindlela.Booking.Api.Common;
using Vulindlela.Booking.Api.Models;
using Vulindlela.Booking.Api.Services;

namespace Vulindlela.Tests.Booking
{
    public class BookingRulesTests
    {
        [Fact]
        public async Task ValidRequest_IsBooked()
        {
            using var database = new TestDatabase();
            var slotId = await database.AddSlotAsync(DateTime.UtcNow.AddDays(1));
            using var db = database.CreateContext();

            var result = await TestDatabase.CreateService(db)
                .BookAppointmentAsync(slotId, TestCitizens.Thandi, ServiceTypes.Passport);

            Assert.Equal(BookingOutcome.Booked, result.Outcome);
            Assert.Equal(1, await database.CountActiveAppointmentsAsync(slotId));
        }

        [Theory]
        [InlineData("")]                
        [InlineData("900101500908")]    
        [InlineData("90010150O9086")]  
        [InlineData("9001015009087")]   
        [InlineData("9013015009081")]
        public async Task InvalidCitizenId_IsRejected(string citizenId)
        {
            using var database = new TestDatabase();
            var slotId = await database.AddSlotAsync(DateTime.UtcNow.AddDays(1));
            using var db = database.CreateContext();

            var result = await TestDatabase.CreateService(db)
                .BookAppointmentAsync(slotId, citizenId, ServiceTypes.Passport);

            Assert.Equal(BookingOutcome.InvalidCitizenId, result.Outcome);
            Assert.Equal(0, await database.CountActiveAppointmentsAsync(slotId));
        }

        [Fact]
        public async Task UnknownServiceType_IsRejected()
        {
            using var database = new TestDatabase();
            var slotId = await database.AddSlotAsync(DateTime.UtcNow.AddDays(1));
            using var db = database.CreateContext();

            var result = await TestDatabase.CreateService(db)
                .BookAppointmentAsync(slotId, TestCitizens.Thandi, "DRIVERS_LICENCE");

            Assert.Equal(BookingOutcome.UnknownServiceType, result.Outcome);
        }

        [Fact]
        public async Task SlotThatDoesNotExist_IsRejected()
        {
            using var database = new TestDatabase();
            using var db = database.CreateContext();

            var result = await TestDatabase.CreateService(db)
                .BookAppointmentAsync(999_999, TestCitizens.Thandi, ServiceTypes.Passport);

            Assert.Equal(BookingOutcome.SlotNotFound, result.Outcome);
        }

        [Fact]
        public async Task SlotThatHasStarted_IsRejected()
        {
            using var database = new TestDatabase();
            var slotId = await database.AddSlotAsync(DateTime.UtcNow.AddHours(-1));
            using var db = database.CreateContext();

            var result = await TestDatabase.CreateService(db)
                .BookAppointmentAsync(slotId, TestCitizens.Thandi, ServiceTypes.Passport);

            Assert.Equal(BookingOutcome.SlotInPast, result.Outcome);
            Assert.Equal(0, await database.CountActiveAppointmentsAsync(slotId));
        }

        [Fact]
        public async Task CancellingTwice_ReportsAlreadyCancelled()
        {
            using var database = new TestDatabase();
            var slotId = await database.AddSlotAsync(DateTime.UtcNow.AddDays(1));
            using var db = database.CreateContext();
            var service = TestDatabase.CreateService(db);
            var booked = await service.BookAppointmentAsync(slotId, TestCitizens.Thandi, ServiceTypes.Passport);

            await service.CancelAppointmentAsync(booked.Appointment!.Id);
            var second = await service.CancelAppointmentAsync(booked.Appointment.Id);

            Assert.Equal(CancelOutcome.AlreadyCancelled, second);
        }

        [Fact]
        public async Task AvailableSlots_LeaveOutBookedSlots()
        {
            using var database = new TestDatabase();
            var bookedSlotId = await database.AddSlotAsync(DateTime.UtcNow.AddDays(1));
            using var db = database.CreateContext();
            var service = TestDatabase.CreateService(db);
            await service.BookAppointmentAsync(bookedSlotId, TestCitizens.Thandi, ServiceTypes.Passport);
            var branchId = (await db.Slots.FindAsync(bookedSlotId))!.BranchId;
            var freeSlot = new Slot { BranchId = branchId, StartsAt = DateTime.UtcNow.AddDays(1).AddHours(1) };
            db.Slots.Add(freeSlot);
            await db.SaveChangesAsync();

            var available = await service.GetAvailableSlotsAsync(branchId, Utility.LocalToday(DateTime.UtcNow), days: 3);

            Assert.NotNull(available);
            Assert.Equal(new[] { freeSlot.Id }, available.Select(s => s.Id));
        }
    }
}
