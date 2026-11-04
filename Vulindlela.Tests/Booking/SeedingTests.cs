using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Data.Seedings;

namespace Vulindlela.Tests.Booking
{
    public class SeedingTests
    {
        [Fact]
        public async Task FirstRun_SeedsAtLeastThreeBranchesWithTwoWeeksOfFutureSlots()
        {
            using var database = new TestDatabase();
            using var db = database.CreateContext();

            await new BookingSeeder(db).SeedAsync();

            var slots = await db.Slots.ToListAsync();
            Assert.True(await db.Branches.CountAsync() >= 3);
            Assert.All(slots, s => Assert.True(s.StartsAt > DateTime.UtcNow));
            Assert.True(slots.Max(s => s.StartsAt) - slots.Min(s => s.StartsAt) >= TimeSpan.FromDays(14));
        }
    }
}
