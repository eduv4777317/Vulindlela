using Microsoft.AspNetCore.Mvc;
using Vulindlela.Clearance.Api.Contracts;
using Vulindlela.Clearance.Api.Services;

namespace Vulindlela.Clearance.Api.Controllers
{
    [ApiController]
    [Route("api/requirements")]
    public class RequirementsController : ControllerBase
    {
        private readonly IClearanceService _clearanceService;

        public RequirementsController(IClearanceService clearanceService)
        {
            _clearanceService = clearanceService;
        }

        [HttpGet("{serviceType}")]
        public async Task<ActionResult<IEnumerable<RequirementResponse>>> GetRequirements(string serviceType)
        {
            var requirements = await _clearanceService.GetRequirementsAsync(serviceType);
            if (requirements.Count == 0)
                return NotFound();

            return Ok(requirements.Select(r => r.ToResponse()));
        }
    }
}
