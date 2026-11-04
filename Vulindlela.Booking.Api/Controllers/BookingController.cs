using Microsoft.AspNetCore.Mvc;
using Vulindlela.Booking.Api.Contracts;
using Vulindlela.Booking.Api.Models;
using Vulindlela.Booking.Api.Services;

namespace Vulindlela.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost]
        public async Task<ActionResult<AppointmentResponse>> Book([FromBody] BookAppointmentRequest request)
        {
            var result = await _bookingService.BookAppointmentAsync(
                request.SlotId!.Value, request.CitizenId!, request.ServiceType!);

            return result.Outcome switch
            {
                BookingOutcome.Booked => CreatedAtAction(
                    nameof(GetById), new { id = result.Appointment!.Id },
                    result.Appointment.ToResponse(await SlotOfAsync(result.Appointment.SlotId))),
                BookingOutcome.InvalidCitizenId => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "The ID number is not a valid South African ID number."),
                BookingOutcome.UnknownServiceType => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: $"Unknown service type. Valid types: {string.Join(", ", ServiceTypes.All)}."),
                BookingOutcome.SlotNotFound => Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "That slot does not exist."),
                BookingOutcome.SlotInPast => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "That slot has already started and can no longer be booked."),
                BookingOutcome.SlotAlreadyTaken => Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "That slot has just been taken. Please choose another."),
                _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
            };
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AppointmentResponse>> GetById(int id)
        {
            var appointment = await _bookingService.GetAppointmentAsync(id);
            if (appointment is null)
            {
                return NotFound();
            }

            return Ok(appointment.ToResponse(await SlotOfAsync(appointment.SlotId)));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppointmentResponse>>> GetForCitizen([FromQuery] string citizenId)
        {
            var appointments = await _bookingService.GetAppointmentsForCitizenAsync(citizenId);
            var slots = (await _bookingService.GetSlotsAsync(appointments.Select(a => a.SlotId).Distinct().ToList()))
                .ToDictionary(s => s.Id);
            return Ok(appointments.Select(a => a.ToResponse(slots.GetValueOrDefault(a.SlotId))));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var outcome = await _bookingService.CancelAppointmentAsync(id);

            return outcome switch
            {
                CancelOutcome.Cancelled => NoContent(),
                CancelOutcome.NotFound => NotFound(),
                CancelOutcome.AlreadyCancelled => Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: $"Appointment is already cancelled."),
                CancelOutcome.AlreadyStarted => Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Appointment has already started and can no longer be cancelled."),
                _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
            };
        }

        private async Task<Slot?> SlotOfAsync(int slotId) =>
            (await _bookingService.GetSlotsAsync(new[] { slotId })).FirstOrDefault();
    }
}
