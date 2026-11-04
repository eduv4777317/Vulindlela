using System.Globalization;
using Vulindlela.Booking.Api.Clients;
using Vulindlela.Booking.Api.Common;
using Vulindlela.Booking.Api.Data.Repositories;
using Vulindlela.Booking.Api.Models;

namespace Vulindlela.Booking.Api.Services
{
    public class BookingService : IBookingService
    {
        public const int DefaultDaysToShow = 7;
        public const int MaxDaysToShow = 21;

        private readonly IBookingRepository _bookingRepository;
        private readonly IClearanceNotifier _clearanceNotifier;

        public BookingService(IBookingRepository bookingRepository, IClearanceNotifier clearanceNotifier)
        {
            _bookingRepository = bookingRepository;
            _clearanceNotifier = clearanceNotifier;
        }

        public Task<IReadOnlyList<Branch>> GetBranchesAsync()
        {
            return _bookingRepository.GetBranchesAsync();
        }

        public async Task<IReadOnlyList<Slot>?> GetAvailableSlotsAsync(int branchId, DateOnly? fromDate, int days)
        {
            if (await _bookingRepository.GetBranchByIdAsync(branchId) is null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var firstDay = fromDate ?? Utility.LocalToday(now);
            var dayCount = Math.Clamp(days, 1, MaxDaysToShow);

            var fromUtc = Utility.LocalToUtc(firstDay, TimeOnly.MinValue);
            var toUtc = Utility.LocalToUtc(firstDay.AddDays(dayCount), TimeOnly.MinValue);

            if (fromUtc < now)
            {
                fromUtc = now;
            }

            if (toUtc <= fromUtc)
            {
                return Array.Empty<Slot>();
            }

            var slots = await _bookingRepository.GetSlotsForBranchAsync(branchId, fromUtc, toUtc);
            if (slots.Count == 0)
            {
                return slots;
            }

            var appointments = await _bookingRepository.GetAppointmentsForSlotsAsync(slots.Select(s => s.Id).ToList());
            var takenSlotIds = appointments
                .Where(IsActive)
                .Select(a => a.SlotId)
                .ToHashSet();

            return slots.Where(s => !takenSlotIds.Contains(s.Id)).ToList();
        }

        public async Task<BookingResult> BookAppointmentAsync(int slotId, string citizenId, string serviceType)
        {
            var normalisedCitizenId = citizenId?.Trim() ?? "";
            if (!IsValidCitizenId(normalisedCitizenId))
            {
                return BookingResult.Rejected(BookingOutcome.InvalidCitizenId);
            }

            var normalisedServiceType = serviceType?.Trim().ToUpperInvariant() ?? "";
            if (!ServiceTypes.All.Contains(normalisedServiceType))
            {
                return BookingResult.Rejected(BookingOutcome.UnknownServiceType);
            }

            var slot = await _bookingRepository.GetSlotByIdAsync(slotId);
            if (slot is null)
            {
                return BookingResult.Rejected(BookingOutcome.SlotNotFound);
            }

            if (slot.StartsAt <= DateTime.UtcNow)
            {
                return BookingResult.Rejected(BookingOutcome.SlotInPast);
            }

            if (await IsSlotTakenAsync(slotId))
            {
                return BookingResult.Rejected(BookingOutcome.SlotAlreadyTaken);
            }

            var appointment = new Appointment
            {
                SlotId = slotId,
                CitizenId = normalisedCitizenId,
                ServiceType = normalisedServiceType,
                Status = AppointmentStatus.Active
            };

            var inserted = await _bookingRepository.InsertAppointmentAsync(appointment);
            if (inserted != InsertAppointmentResult.Inserted)
            {
                return BookingResult.Rejected(BookingOutcome.SlotAlreadyTaken);
            }

            if (await _clearanceNotifier.NotifyAppointmentCreatedAsync(appointment, slot))
            {
                appointment.ClearanceNotifiedAt = DateTime.UtcNow;
                await _bookingRepository.SaveChangesAsync();
            }

            return BookingResult.Booked(appointment);
        }

        public Task<Appointment?> GetAppointmentAsync(int id)
        {
            return _bookingRepository.GetAppointmentByIdAsync(id);
        }

        public Task<IReadOnlyList<Appointment>> GetAppointmentsForCitizenAsync(string citizenId)
        {
            return _bookingRepository.GetAppointmentsForCitizenAsync(citizenId?.Trim() ?? "");
        }

        public Task<IReadOnlyList<Slot>> GetSlotsAsync(IReadOnlyCollection<int> slotIds)
        {
            return _bookingRepository.GetSlotsByIdsAsync(slotIds);
        }

        public async Task<CancelOutcome> CancelAppointmentAsync(int id)
        {
            var appointment = await _bookingRepository.GetAppointmentByIdAsync(id);
            if (appointment is null)
            {
                return CancelOutcome.NotFound;
            }

            if (!IsActive(appointment))
            {
                return CancelOutcome.AlreadyCancelled;
            }

            var slot = await _bookingRepository.GetSlotByIdAsync(appointment.SlotId);
            if (slot is not null && slot.StartsAt <= DateTime.UtcNow)
            {
                return CancelOutcome.AlreadyStarted;
            }

            appointment.Status = AppointmentStatus.Cancelled;
            await _bookingRepository.SaveChangesAsync();
            return CancelOutcome.Cancelled;
        }

        private async Task<bool> IsSlotTakenAsync(int slotId)
        {
            var appointments = await _bookingRepository.GetAppointmentsForSlotAsync(slotId);
            return appointments.Any(IsActive);
        }

        private static bool IsActive(Appointment appointment) =>
            appointment.Status == AppointmentStatus.Active;

        public static bool IsValidCitizenId(string idNumber)
        {
            if (idNumber.Length != 13 || !idNumber.All(char.IsAsciiDigit))
            {
                return false;
            }

            if (!DateOnly.TryParseExact(idNumber[..6], "yyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _))
            {
                return false;
            }

            var sum = 0;
            for (var position = 0; position < idNumber.Length; position++)
            {
                var digit = idNumber[idNumber.Length - 1 - position] - '0';
                if (position % 2 == 1)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }
                sum += digit;
            }

            return sum % 10 == 0;
        }
    }
}
