using Microsoft.AspNetCore.Mvc;
using Vulindlela.Portal.Mvc.Clients;
using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly IBookingApiClient _bookingApi;
        private readonly IClearanceApiClient _clearanceApi;

        public AppointmentsController(IBookingApiClient bookingApi, IClearanceApiClient clearanceApi)
        {
            _bookingApi = bookingApi;
            _clearanceApi = clearanceApi;
        }

        public async Task<IActionResult> Index(string? citizenId)
        {
            if (string.IsNullOrWhiteSpace(citizenId))
            {
                return View(new MyAppointmentsViewModel());
            }

            var appointments = await _bookingApi.GetAppointmentsAsync(citizenId);
            if (!appointments.Succeeded)
            {
                return View(new MyAppointmentsViewModel { CitizenId = citizenId, BookingUnavailable = true });
            }

            var branches = await _bookingApi.GetBranchesAsync();
            var branchNames = (branches.Value ?? new List<BranchDto>()).ToDictionary(b => b.Id, b => b.Name);

            var ids = appointments.Value!.Select(a => a.Id).ToList();
            var clearances = ids.Count == 0
                ? new ApiResult<List<ClearanceDto>>(true, new List<ClearanceDto>(), 200)
                : await _clearanceApi.GetClearancesAsync(ids);
            var clearanceById = (clearances.Value ?? new List<ClearanceDto>()).ToDictionary(c => c.AppointmentId);

            var rows = appointments.Value!
                .OrderBy(a => a.StartsAt)
                .Select(a => new AppointmentRow
                {
                    Appointment = a,
                    BranchName = a.BranchId is int id && branchNames.TryGetValue(id, out var name) ? name : "",
                    Clearance = clearanceById.GetValueOrDefault(a.Id)
                })
                .ToList();

            return View(new MyAppointmentsViewModel
            {
                CitizenId = citizenId,
                ClearanceUnavailable = !clearances.Succeeded,
                Rows = rows
            });
        }

        [HttpGet]
        public async Task<IActionResult> Documents(int id, string citizenId)
        {
            var model = new DocumentsViewModel { AppointmentId = id, CitizenId = citizenId };
            var clearance = await _clearanceApi.GetClearanceAsync(id);

            if (!clearance.Available)
            {
                model.ClearanceUnavailable = true;
            }
            else if (!clearance.Succeeded)
            {
                model.Error = "Clearance has no record of this appointment yet. Please try again shortly.";
            }
            else
            {
                model.Rows = clearance.Value!.RequiredDocuments
                    .Select(r => new DocumentRow { DocumentType = r.DocumentType, MustBeCertified = r.MustBeCertified })
                    .ToList();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Documents(DocumentsViewModel model)
        {
            var documents = model.Rows
                .Where(r => r.Include)
                .Select(r => new SubmittedDocumentDto(r.DocumentType, r.CertifiedOn, r.ExpiresOn))
                .ToList();

            var result = await _clearanceApi.SubmitAsync(model.AppointmentId, documents);
            if (result.Succeeded)
            {
                TempData["Message"] = "Your documents were received and are being checked. This page updates by itself.";
                return RedirectToAction(nameof(Index), new { citizenId = model.CitizenId });
            }

            model.ClearanceUnavailable = !result.Available;
            model.Error = result.Available ? result.Error ?? "Your documents could not be submitted." : null;
            return View(model);
        }
    }
}
