using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Clients
{
    public interface IClearanceApiClient
    {
        Task<ApiResult<List<ClearanceDto>>> GetClearancesAsync(IReadOnlyCollection<int> appointmentIds);
        Task<ApiResult<ClearanceDto>> GetClearanceAsync(int appointmentId);
        Task<ApiResult<SubmissionDto>> SubmitAsync(int appointmentId, IReadOnlyList<SubmittedDocumentDto> documents);
    }
}
