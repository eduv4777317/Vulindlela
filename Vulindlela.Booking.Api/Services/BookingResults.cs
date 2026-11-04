using Vulindlela.Booking.Api.Models;

namespace Vulindlela.Booking.Api.Services
{
    public enum BookingOutcome
    {
        Booked,
        InvalidCitizenId,
        UnknownServiceType,
        SlotNotFound,
        SlotInPast,
        SlotAlreadyTaken
    }

    public sealed record BookingResult(BookingOutcome Outcome, Appointment? Appointment = null)
    {
        public static BookingResult Booked(Appointment appointment) => new(BookingOutcome.Booked, appointment);
        public static BookingResult Rejected(BookingOutcome outcome) => new(outcome);
    }

    public enum CancelOutcome
    {
        Cancelled,
        NotFound,
        AlreadyCancelled,
        AlreadyStarted
    }
}
