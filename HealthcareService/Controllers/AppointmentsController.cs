using Microsoft.AspNetCore.Mvc;
using HealthcareService.DTOs;
using HealthcareService.Services;

namespace HealthcareService.Controllers
{
    public class AppointmentApprovalDto
    {
        public string? Notes { get; set; }
    }

    [ApiController]
    [Route("api/healthcare/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments(
            [FromQuery] int? doctorId,
            [FromQuery] int? patientId,
            [FromQuery] string? status)
        {
            var appointments = await _appointmentService.GetAppointmentsAsync(doctorId, patientId, status);
            return Ok(appointments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAppointmentById(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                return NotFound(new { message = "Appointment not found." });
            }
            return Ok(appointment);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _appointmentService.CreateAppointmentAsync(dto);
            return CreatedAtAction(nameof(GetAppointmentById), new { id = created.AppointmentId }, created);
        }

        // Nurse approval: transitions appointment from Requested -> NurseApproved
        [HttpPut("{id}/nurse-approve")]
        public async Task<IActionResult> NurseApprove(int id, [FromBody] AppointmentApprovalDto? dto)
        {
            var success = await _appointmentService.NurseApproveAsync(id, dto?.Notes);
            if (!success)
            {
                return NotFound(new { message = "Appointment not found." });
            }
            return Ok(new { message = "Appointment approved by nurse and forwarded to doctor." });
        }

        // Doctor confirmation: transitions appointment from NurseApproved or Rescheduled -> DoctorConfirmed
        [HttpPut("{id}/doctor-confirm")]
        public async Task<IActionResult> DoctorConfirm(int id, [FromBody] AppointmentApprovalDto? dto)
        {
            var success = await _appointmentService.DoctorConfirmAsync(id, dto?.Notes);
            if (!success)
            {
                return NotFound(new { message = "Appointment not found or not eligible for confirmation." });
            }
            return Ok(new { message = "Appointment confirmed by doctor." });
        }

        // Cancel appointment (Doctor declining a rescheduled appointment)
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id, [FromBody] AppointmentApprovalDto? dto)
        {
            var success = await _appointmentService.CancelAppointmentAsync(id, dto?.Notes);
            if (!success)
            {
                return NotFound(new { message = "Appointment not found." });
            }
            return Ok(new { message = "Appointment cancelled." });
        }

        // Reschedule appointment
        [HttpPut("{id}/reschedule")]
        public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleAppointmentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _appointmentService.RescheduleAsync(id, dto);
            if (!success)
            {
                return NotFound(new { message = "Appointment not found." });
            }
            return Ok(new { message = "Appointment rescheduled successfully." });
        }
    }
}
