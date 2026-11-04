using Microsoft.AspNetCore.Mvc;
using Vulindlela.Booking.Api.Contracts;
using Vulindlela.Booking.Api.Services;

namespace Vulindlela.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/access-log")]
    public class AccessLogController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public AccessLogController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccessLogEntryResponse>>> GetForCitizen([FromQuery] string citizenId)
        {
            var entries = await _bookingService.GetAccessLogAsync(citizenId);
            return Ok(entries.Select(e => e.ToResponse()));
        }
    }
}
