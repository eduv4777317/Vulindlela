namespace Vulindlela.Booking.Api.Common
{
    public static class Utility
    {
        public static readonly TimeSpan UtcOffset = TimeSpan.FromHours(2);

        public static DateTimeOffset ToLocal(DateTime utc) =>
            new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToOffset(UtcOffset);

        public static DateOnly LocalToday(DateTime utcNow) =>
            DateOnly.FromDateTime(utcNow + UtcOffset);

        public static DateTime LocalToUtc(DateOnly date, TimeOnly time) =>
            DateTime.SpecifyKind(date.ToDateTime(time) - UtcOffset, DateTimeKind.Utc);
    }
}
