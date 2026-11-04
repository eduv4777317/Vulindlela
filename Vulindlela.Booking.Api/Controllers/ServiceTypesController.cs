using Microsoft.AspNetCore.Mvc;
using Vulindlela.Booking.Api.Services;

namespace Vulindlela.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/service-types")]
    public class ServiceTypesController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<string>> GetServiceTypes()
        {
            return Ok(ServiceTypes.All.OrderBy(t => t));
        }
    }
}
