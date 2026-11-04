using Microsoft.AspNetCore.Mvc;
using Vulindlela.Clearance.Api.Common;
using Vulindlela.Clearance.Api.Contracts;
using Vulindlela.Clearance.Api.Services;

namespace Vulindlela.Clearance.Api.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IClearanceService _clearanceService;

        public AppointmentsController(IClearanceService clearanceService)
        {
            _clearanceService = clearanceService;
        }

        [HttpPost]
        public async Task<IActionResult> AppointmentCreated([FromBody] AppointmentCreatedRequest request)
        {
            var created = await _clearanceService.RegisterAppointmentAsync(
                request.AppointmentId!.Value, request.CitizenId!, request.ServiceType!, request.StartsAt!.Value.ToUniversalTime());

            return created ? StatusCode(StatusCodes.Status201Created) : Ok();
        }

        [HttpGet("{appointmentId:int}/clearance")]
        public async Task<ActionResult<ClearanceResponse>> GetClearance(int appointmentId)
        {
            var status = (await _clearanceService.GetClearancesAsync(new[] { appointmentId }, AccessHeaders.FromRequest(Request))).Single();
            if (!status.AppointmentKnown)
            {
                return NotFound();
            }

            return Ok(status.ToResponse());
        }

        [HttpPost("{appointmentId:int}/submissions")]
        public async Task<ActionResult<SubmissionResponse>> Submit(int appointmentId, [FromBody] SubmitDocumentsRequest request)
        {
            var documents = request.Documents!
                .Where(d => !string.IsNullOrWhiteSpace(d.DocumentType))
                .Select(d => new DocumentInput(d.DocumentType!, d.CertifiedOn, d.ExpiresOn))
                .ToList();

            var result = await _clearanceService.SubmitDocumentsAsync(appointmentId, documents);

            return result.Outcome switch
            {
                SubmitOutcome.Queued => AcceptedAtAction(
                    nameof(GetClearance), new { appointmentId }, result.Submission!.ToResponse()),
                SubmitOutcome.AppointmentNotFound => Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Clearance has no record of that appointment yet."),
                _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
            };
        }
    }
}
