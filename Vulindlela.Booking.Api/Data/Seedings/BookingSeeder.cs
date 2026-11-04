using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Common;
using Vulindlela.Booking.Api.Models;

namespace Vulindlela.Booking.Api.Data.Seedings
{
    public class BookingSeeder : IBookingSeeder
    {
        public static readonly IReadOnlyList<string> BranchNames = new[]
        {
            "Johannesburg Central",
            "Pretoria Central",
            "Cape Town City Centre",
            "Durban Central"
        };

        public const int WorkingDaysToSeed = 15;

        public static readonly TimeOnly FirstSlot = new(8, 0);
        public static readonly TimeOnly LastSlot = new(15, 30);
        public static readonly TimeSpan SlotLength = TimeSpan.FromMinutes(30);

        private readonly BookingDBContext _bookingDBContext;

        public BookingSeeder(BookingDBContext bookingDBContext)
        {
            _bookingDBContext = bookingDBContext;
        }

        public async Task SeedAsync()
        {
            if (await _bookingDBContext.Branches.AnyAsync())
            {
                return;
            }

            var branches = BranchNames.Select(name => new Branch { Name = name }).ToList();
            _bookingDBContext.Branches.AddRange(branches);
            await _bookingDBContext.SaveChangesAsync();

            var slots = new List<Slot>();
            foreach (var day in WorkingDays(Utility.LocalToday(DateTime.UtcNow).AddDays(1), WorkingDaysToSeed))
            {
                foreach (var branch in branches)
                {
                    for (var time = FirstSlot; time <= LastSlot; time = time.Add(SlotLength))
                    {
                        slots.Add(new Slot
                        {
                            BranchId = branch.Id,
                            StartsAt = Utility.LocalToUtc(day, time)
                        });
                    }
                }
            }

            _bookingDBContext.Slots.AddRange(slots);
            await _bookingDBContext.SaveChangesAsync();
        }

        private static IEnumerable<DateOnly> WorkingDays(DateOnly start, int count)
        {
            var day = start;
            var produced = 0;
            while (produced < count)
            {
                if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                {
                    yield return day;
                    produced++;
                }
                day = day.AddDays(1);
            }
        }
    }
}
