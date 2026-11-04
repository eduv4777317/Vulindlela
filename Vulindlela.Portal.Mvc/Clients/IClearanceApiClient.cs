using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Clients
{
    public interface IClearanceApiClient
    {
        Task<ApiResult<List<ClearanceDto>>> GetClearancesAsync(IReadOnlyCollection<int> appointmentIds, string reason);
        Task<ApiResult<ClearanceDto>> GetClearanceAsync(int appointmentId, string reason);
        Task<ApiResult<List<AccessLogEntryDto>>> GetAccessLogAsync(string citizenId);
        Task<ApiResult<SubmissionDto>> SubmitAsync(int appointmentId, IReadOnlyList<SubmittedDocumentDto> documents);
    }
}
