using Vulindlela.Clearance.Api.Services;
using Vulindlela.Tests.Booking;

namespace Vulindlela.Tests.Clearance
{
    public class ClearanceAccessLogTests
    {
        private static readonly DateTime AppointmentAt = new(2026, 12, 1, 6, 0, 0, DateTimeKind.Utc);
        private static readonly AccessContext Official = new("Agency Official", "Official", "Opened the documents page");

        [Fact]
        public async Task ReadingClearance_IsLoggedAgainstTheAppointmentsOwner()
        {
            using var database = new ClearanceTestDatabase();
            using var db = database.CreateContext();
            var service = database.CreateService(db);
            await service.RegisterAppointmentAsync(1, TestCitizens.Thandi, "PASSPORT", AppointmentAt);

            await service.GetClearancesAsync(new[] { 1 }, Official);

            var entry = Assert.Single(await service.GetAccessLogAsync(TestCitizens.Thandi));
            Assert.Equal("Agency Official", entry.AccessedBy);
            Assert.Equal("Official", entry.AccessedByRole);
            Assert.Equal("Opened the documents page", entry.Reason);
        }

        [Fact]
        public async Task ReadingSeveralAppointments_LogsOncePerCitizen()
        {
            using var database = new ClearanceTestDatabase();
            using var db = database.CreateContext();
            var service = database.CreateService(db);
            await service.RegisterAppointmentAsync(1, TestCitizens.Thandi, "PASSPORT", AppointmentAt);
            await service.RegisterAppointmentAsync(2, TestCitizens.Thandi, "PASSPORT", AppointmentAt.AddDays(1));
            await service.RegisterAppointmentAsync(3, TestCitizens.Sipho, "PASSPORT", AppointmentAt);

            await service.GetClearancesAsync(new[] { 1, 2, 3 }, Official);

            Assert.Single(await service.GetAccessLogAsync(TestCitizens.Thandi));
            Assert.Single(await service.GetAccessLogAsync(TestCitizens.Sipho));
        }

        [Fact]
        public async Task ReadingAnUnknownAppointment_LogsNothing()
        {
            using var database = new ClearanceTestDatabase();
            using var db = database.CreateContext();
            var service = database.CreateService(db);

            await service.GetClearancesAsync(new[] { 42 }, Official);

            Assert.Empty(db.AccessLog);
        }
    }
}
