using System.Net.Http.Json;
using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Clients
{
    public class BookingApiClient : IBookingApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BookingApiClient> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BookingApiClient(HttpClient httpClient, ILogger<BookingApiClient> logger, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<ApiResult<List<BranchDto>>> GetBranchesAsync() =>
            ApiCallHelper.SendAsync<List<BranchDto>>(() => _httpClient.GetAsync("api/branches"), _logger);

        public Task<ApiResult<List<string>>> GetServiceTypesAsync() =>
            ApiCallHelper.SendAsync<List<string>>(() => _httpClient.GetAsync("api/service-types"), _logger);

        public Task<ApiResult<List<SlotDto>>> GetAvailableSlotsAsync(int branchId, int days) =>
            ApiCallHelper.SendAsync<List<SlotDto>>(() => _httpClient.GetAsync($"api/branches/{branchId}/slots?days={days}"), _logger);

        public Task<ApiResult<AppointmentDto>> BookAsync(BookAppointmentDto request) =>
            ApiCallHelper.SendAsync<AppointmentDto>(() => _httpClient.PostAsJsonAsync("api/appointments", request), _logger);

        public Task<ApiResult<AppointmentDto>> GetAppointmentAsync(int id, string reason) =>
            ApiCallHelper.SendAsync<AppointmentDto>(
                () => _httpClient.SendAsync(ApiCallHelper.CreateRead($"api/appointments/{id}", _httpContextAccessor.HttpContext?.User, reason)), _logger);

        public Task<ApiResult<List<AppointmentDto>>> GetAppointmentsAsync(string citizenId, string reason) =>
            ApiCallHelper.SendAsync<List<AppointmentDto>>(
                () => _httpClient.SendAsync(ApiCallHelper.CreateRead(
                    $"api/appointments?citizenId={Uri.EscapeDataString(citizenId)}", _httpContextAccessor.HttpContext?.User, reason)), _logger);

        public Task<ApiResult<List<AccessLogEntryDto>>> GetAccessLogAsync(string citizenId) =>
            ApiCallHelper.SendAsync<List<AccessLogEntryDto>>(
                () => _httpClient.GetAsync($"api/access-log?citizenId={Uri.EscapeDataString(citizenId)}"), _logger);
    }
}
