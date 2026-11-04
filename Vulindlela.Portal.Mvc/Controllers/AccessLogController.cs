using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Portal.Mvc.Clients;
using Vulindlela.Portal.Mvc.Models;
using Vulindlela.Portal.Mvc.Security;

namespace Vulindlela.Portal.Mvc.Controllers
{
    [Authorize]
    public class AccessLogController : Controller
    {
        private readonly IBookingApiClient _bookingApi;
        private readonly IClearanceApiClient _clearanceApi;
        private readonly IAuthorizationService _authorizationService;

        public AccessLogController(
            IBookingApiClient bookingApi,
            IClearanceApiClient clearanceApi,
            IAuthorizationService authorizationService)
        {
            _bookingApi = bookingApi;
            _clearanceApi = clearanceApi;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> Index(string? citizenId)
        {
            var isOfficial = User.IsInRole(Roles.Official);
            var requestedCitizenId = string.IsNullOrWhiteSpace(citizenId) ? User.GetCitizenId() : citizenId.Trim();

            if (requestedCitizenId is null)
            {
                return View(new AccessLogViewModel { IsOfficial = isOfficial });
            }

            var authorization = await _authorizationService.AuthorizeAsync(User, requestedCitizenId, Policies.CanViewCitizenRecord);
            if (!authorization.Succeeded)
            {
                return Forbid();
            }

            var bookingLog = await _bookingApi.GetAccessLogAsync(requestedCitizenId);
            var clearanceLog = await _clearanceApi.GetAccessLogAsync(requestedCitizenId);

            var rows = ToRows(bookingLog, "Booking")
                .Concat(ToRows(clearanceLog, "Document clearance"))
                .OrderByDescending(r => r.AccessedAt)
                .ToList();

            return View(new AccessLogViewModel
            {
                IsOfficial = isOfficial,
                CitizenId = requestedCitizenId,
                BookingUnavailable = !bookingLog.Succeeded,
                ClearanceUnavailable = !clearanceLog.Succeeded,
                Rows = rows
            });
        }

        private static IEnumerable<AccessLogRow> ToRows(ApiResult<List<AccessLogEntryDto>> result, string source) =>
            (result.Value ?? new List<AccessLogEntryDto>()).Select(e => new AccessLogRow
            {
                AccessedAt = e.AccessedAt,
                AccessedBy = e.AccessedBy,
                AccessedByRole = e.AccessedByRole,
                Reason = e.Reason,
                Source = source
            });
    }
}
