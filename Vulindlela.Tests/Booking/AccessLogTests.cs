using Vulindlela.Booking.Api.Services;

namespace Vulindlela.Tests.Booking
{
    public class AccessLogTests
    {
        private static readonly AccessContext Official = new("Agency Official", "Official", "Viewed appointments");

        [Fact]
        public async Task ReadingACitizensAppointments_RecordsWhoWhenAndWhy()
        {
            using var database = new TestDatabase();
            using var db = database.CreateContext();
            var service = TestDatabase.CreateService(db);
            var before = DateTime.UtcNow;

            await service.GetAppointmentsForCitizenAsync(TestCitizens.Thandi, Official);

            var entry = Assert.Single(await service.GetAccessLogAsync(TestCitizens.Thandi));
            Assert.Equal("Agency Official", entry.AccessedBy);
            Assert.Equal("Official", entry.AccessedByRole);
            Assert.Equal("Viewed appointments", entry.Reason);
            Assert.True(entry.AccessedAt >= before);
        }

        [Fact]
        public async Task ReadingOneAppointment_IsLoggedAgainstItsOwner()
        {
            using var database = new TestDatabase();
            var slotId = await database.AddSlotAsync(DateTime.UtcNow.AddDays(1));
            using var db = database.CreateContext();
            var service = TestDatabase.CreateService(db);
            var booked = await service.BookAppointmentAsync(slotId, TestCitizens.Sipho, ServiceTypes.Passport);

            await service.GetAppointmentAsync(booked.Appointment!.Id, Official);

            Assert.Single(await service.GetAccessLogAsync(TestCitizens.Sipho));
            Assert.Empty(await service.GetAccessLogAsync(TestCitizens.Thandi));
        }

        [Fact]
        public async Task ReadingAnAppointmentThatDoesNotExist_LogsNothing()
        {
            using var database = new TestDatabase();
            using var db = database.CreateContext();
            var service = TestDatabase.CreateService(db);

            await service.GetAppointmentAsync(999_999, Official);

            Assert.Empty(db.AccessLog);
        }

        [Fact]
        public async Task AccessLog_ShowsOnlyThatCitizen_NewestFirst()
        {
            using var database = new TestDatabase();
            using var db = database.CreateContext();
            var service = TestDatabase.CreateService(db);

            await service.GetAppointmentsForCitizenAsync(TestCitizens.Thandi, new("Thandi Mokoena", "Citizen", "First"));
            await service.GetAppointmentsForCitizenAsync(TestCitizens.Sipho, Official);
            await service.GetAppointmentsForCitizenAsync(TestCitizens.Thandi, new("Thandi Mokoena", "Citizen", "Second"));

            var log = await service.GetAccessLogAsync(TestCitizens.Thandi);

            Assert.Equal(new[] { "Second", "First" }, log.Select(e => e.Reason));
        }
    }
}
