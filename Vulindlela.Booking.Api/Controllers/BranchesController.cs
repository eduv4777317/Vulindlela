using Microsoft.AspNetCore.Mvc;
using Vulindlela.Booking.Api.Contracts;
using Vulindlela.Booking.Api.Services;

namespace Vulindlela.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/branches")]
    public class BranchesController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BranchesController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BranchResponse>>> GetBranches()
        {
            var branches = await _bookingService.GetBranchesAsync();
            return Ok(branches.Select(b => b.ToResponse()));
        }

        [HttpGet("{branchId:int}/slots")]
        public async Task<ActionResult<IEnumerable<SlotResponse>>> GetAvailableSlots(
            int branchId,
            [FromQuery] DateOnly? from,
            [FromQuery] int days = BookingService.DefaultDaysToShow)
        {
            var slots = await _bookingService.GetAvailableSlotsAsync(branchId, from, days);
            if (slots is null)
            {
                return NotFound();
            }

            return Ok(slots.Select(s => s.ToResponse()));
        }
    }
}
