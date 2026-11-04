using System.Net.Http.Json;
using Vulindlela.Booking.Api.Models;

namespace Vulindlela.Booking.Api.Clients
{
    public class ClearanceNotifier : IClearanceNotifier
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClearanceNotifier> _logger;

        public ClearanceNotifier(HttpClient httpClient, ILogger<ClearanceNotifier> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> NotifyAppointmentCreatedAsync(Appointment appointment, Slot slot)
        {
            var notification = new
            {
                AppointmentId = appointment.Id,
                appointment.CitizenId,
                appointment.ServiceType,
                StartsAt = DateTime.SpecifyKind(slot.StartsAt, DateTimeKind.Utc)
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/appointments", notification);
                if (response.IsSuccessStatusCode)
                    return true;

                _logger.LogWarning(
                    "Clearance refused appointment {AppointmentId} with status {StatusCode}.",
                    appointment.Id,
                    (int)response.StatusCode
                );

                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Clearance is unreachable. Appointment {AppointmentId} is booked but not yet sent to Clearance.",
                    appointment.Id
                );

                return false;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Clearance timed out. Appointment {AppointmentId} is booked but not yet sent to Clearance.",
                    appointment.Id
                    );

                return false;
            }
        }
    }
}
