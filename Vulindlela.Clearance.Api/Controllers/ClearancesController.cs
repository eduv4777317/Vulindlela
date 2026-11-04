using Microsoft.AspNetCore.Mvc;
using Vulindlela.Clearance.Api.Contracts;
using Vulindlela.Clearance.Api.Services;

namespace Vulindlela.Clearance.Api.Controllers
{
    [ApiController]
    [Route("api/clearances")]
    public class ClearancesController : ControllerBase
    {
        private readonly IClearanceService _clearanceService;

        public ClearancesController(IClearanceService clearanceService)
        {
            _clearanceService = clearanceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClearanceResponse>>> GetClearances([FromQuery] int[] appointmentIds)
        {
            var statuses = await _clearanceService.GetClearancesAsync(appointmentIds);
            return Ok(statuses.Select(s => s.ToResponse()));
        }
    }
}
