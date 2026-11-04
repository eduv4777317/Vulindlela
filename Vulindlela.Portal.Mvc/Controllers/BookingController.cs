using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Portal.Mvc.Clients;
using Vulindlela.Portal.Mvc.Models;
using Vulindlela.Portal.Mvc.Security;

namespace Vulindlela.Portal.Mvc.Controllers
{
    public class BookingController : Controller
    {
        private const int DaysToShow = 7;
        private static readonly TimeSpan AgencyOffset = TimeSpan.FromHours(2);

        private readonly IBookingApiClient _bookingApi;

        public BookingController(IBookingApiClient bookingApi)
        {
            _bookingApi = bookingApi;
        }

        public async Task<IActionResult> Index(int? branchId)
        {
            var branches = await _bookingApi.GetBranchesAsync();
            if (!branches.Succeeded)
            {
                return View(new BookingIndexViewModel { BookingUnavailable = true });
            }

            var selected = branches.Value!.FirstOrDefault(b => b.Id == branchId);
            var slotsByDay = new List<IGrouping<DateOnly, SlotDto>>();
            var unavailable = false;

            if (selected is not null)
            {
                var slots = await _bookingApi.GetAvailableSlotsAsync(selected.Id, DaysToShow);
                unavailable = !slots.Succeeded;
                slotsByDay = (slots.Value ?? new List<SlotDto>())
                    .GroupBy(s => DateOnly.FromDateTime(s.StartsAt.ToOffset(AgencyOffset).DateTime))
                    .ToList();
            }

            return View(new BookingIndexViewModel
            {
                BookingUnavailable = unavailable,
                Branches = branches.Value!,
                SelectedBranch = selected,
                SlotsByDay = slotsByDay
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Book(int slotId, int branchId, DateTimeOffset startsAt)
        {
            var model = new BookViewModel { SlotId = slotId, BranchId = branchId, StartsAt = startsAt };
            await FillAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Book(BookViewModel model)
        {
            var citizenId = User.GetCitizenId()!;
            var result = await _bookingApi.BookAsync(new BookAppointmentDto(model.SlotId, citizenId, model.ServiceType));

            if (result.Succeeded)
            {
                return View("Confirmation", new ConfirmationViewModel
                {
                    Appointment = result.Value!,
                    BranchName = await BranchNameAsync(model.BranchId)
                });
            }

            model.SlotTaken = result.StatusCode == StatusCodes.Status409Conflict;
            model.Error = !result.Available
                ? "Booking is unavailable right now. Please try again in a few minutes."
                : result.Error ?? "That booking could not be made.";
            await FillAsync(model);
            return View(model);
        }

        private async Task FillAsync(BookViewModel model)
        {
            model.BranchName = await BranchNameAsync(model.BranchId);
            model.ServiceTypes = (await _bookingApi.GetServiceTypesAsync()).Value ?? new List<string>();
        }

        private async Task<string> BranchNameAsync(int branchId)
        {
            var branches = await _bookingApi.GetBranchesAsync();
            return branches.Value?.FirstOrDefault(b => b.Id == branchId)?.Name ?? $"Branch {branchId}";
        }
    }
}
