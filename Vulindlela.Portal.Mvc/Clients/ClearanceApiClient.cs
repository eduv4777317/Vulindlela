using System.Net.Http.Json;
using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Clients
{
    public class ClearanceApiClient : IClearanceApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClearanceApiClient> _logger;

        public ClearanceApiClient(HttpClient httpClient, ILogger<ClearanceApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public Task<ApiResult<List<ClearanceDto>>> GetClearancesAsync(IReadOnlyCollection<int> appointmentIds)
        {
            var query = string.Join("&", appointmentIds.Select(id => $"appointmentIds={id}"));
            return ApiCallHelper.SendAsync<List<ClearanceDto>>(() => _httpClient.GetAsync($"api/clearances?{query}"), _logger);
        }

        public Task<ApiResult<ClearanceDto>> GetClearanceAsync(int appointmentId) =>
            ApiCallHelper.SendAsync<ClearanceDto>(() => _httpClient.GetAsync($"api/appointments/{appointmentId}/clearance"), _logger);

        public Task<ApiResult<SubmissionDto>> SubmitAsync(int appointmentId, IReadOnlyList<SubmittedDocumentDto> documents) =>
            ApiCallHelper.SendAsync<SubmissionDto>(
                () => _httpClient.PostAsJsonAsync($"api/appointments/{appointmentId}/submissions", new { documents }), _logger);
    }
}
