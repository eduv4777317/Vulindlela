using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Clients
{
    public interface IBookingApiClient
    {
        Task<ApiResult<List<BranchDto>>> GetBranchesAsync();
        Task<ApiResult<List<string>>> GetServiceTypesAsync();
        Task<ApiResult<List<SlotDto>>> GetAvailableSlotsAsync(int branchId, int days);
        Task<ApiResult<AppointmentDto>> BookAsync(BookAppointmentDto request);
        Task<ApiResult<AppointmentDto>> GetAppointmentAsync(int id, string reason);
        Task<ApiResult<List<AppointmentDto>>> GetAppointmentsAsync(string citizenId, string reason);
        Task<ApiResult<List<AccessLogEntryDto>>> GetAccessLogAsync(string citizenId);
    }
}
