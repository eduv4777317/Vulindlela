using Microsoft.AspNetCore.Mvc;
using Vulindlela.Clearance.Api.Contracts;
using Vulindlela.Clearance.Api.Services;

namespace Vulindlela.Clearance.Api.Controllers
{
    [ApiController]
    [Route("api/access-log")]
    public class AccessLogController : ControllerBase
    {
        private readonly IClearanceService _clearanceService;

        public AccessLogController(IClearanceService clearanceService)
        {
            _clearanceService = clearanceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccessLogEntryResponse>>> GetForCitizen([FromQuery] string citizenId)
        {
            var entries = await _clearanceService.GetAccessLogAsync(citizenId);
            return Ok(entries.Select(e => e.ToResponse()));
        }
    }
}
