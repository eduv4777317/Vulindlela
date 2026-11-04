using System.Net.Http.Json;
using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Clients
{
    public class ClearanceApiClient : IClearanceApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClearanceApiClient> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClearanceApiClient(HttpClient httpClient, ILogger<ClearanceApiClient> logger, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<ApiResult<List<ClearanceDto>>> GetClearancesAsync(IReadOnlyCollection<int> appointmentIds, string reason)
        {
            var query = string.Join("&", appointmentIds.Select(id => $"appointmentIds={id}"));
            return ApiCallHelper.SendAsync<List<ClearanceDto>>(
                () => _httpClient.SendAsync(ApiCallHelper.CreateRead($"api/clearances?{query}", _httpContextAccessor.HttpContext?.User, reason)), _logger);
        }

        public Task<ApiResult<ClearanceDto>> GetClearanceAsync(int appointmentId, string reason) =>
            ApiCallHelper.SendAsync<ClearanceDto>(
                () => _httpClient.SendAsync(ApiCallHelper.CreateRead(
                    $"api/appointments/{appointmentId}/clearance", _httpContextAccessor.HttpContext?.User, reason)), _logger);

        public Task<ApiResult<List<AccessLogEntryDto>>> GetAccessLogAsync(string citizenId) =>
            ApiCallHelper.SendAsync<List<AccessLogEntryDto>>(
                () => _httpClient.GetAsync($"api/access-log?citizenId={Uri.EscapeDataString(citizenId)}"), _logger);

        public Task<ApiResult<SubmissionDto>> SubmitAsync(int appointmentId, IReadOnlyList<SubmittedDocumentDto> documents) =>
            ApiCallHelper.SendAsync<SubmissionDto>(
                () => _httpClient.PostAsJsonAsync($"api/appointments/{appointmentId}/submissions", new { documents }), _logger);
    }
}
