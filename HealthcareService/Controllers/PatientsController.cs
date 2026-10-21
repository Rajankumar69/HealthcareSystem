using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthcareService.DTOs;
using HealthcareService.Services;

namespace HealthcareService.Controllers
{
    [ApiController]
    [Route("api/healthcare/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        // 1. Paginated patient list for Doctor and Nurse
        [HttpGet]
        public async Task<IActionResult> GetPaginatedPatients([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
        {
            var result = await _patientService.GetPaginatedPatientsAsync(pageNumber, pageSize, searchTerm);
            return Ok(result);
        }

        // 2. Get single patient by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatientById(int id)
        {
            var patient = await _patientService.GetPatientByIdAsync(id);
            if (patient == null)
            {
                return NotFound(new { message = "Patient not found or has been deleted." });
            }
            return Ok(patient);
        }

        // 3. Get patient profile by linked User ID (for logged-in Patient)
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetPatientByUserId(int userId)
        {
            var patient = await _patientService.GetPatientByUserIdAsync(userId);
            if (patient == null)
            {
                return NotFound(new { message = "Patient profile not yet created." });
            }
            return Ok(patient);
        }

        // 4. Get Full 3-Section Patient Details (1. Details, 2. Vitals, 3. Medical History)
        [HttpGet("{id}/full-details")]
        public async Task<IActionResult> GetPatientFullDetails(int id, [FromQuery] int? appointmentId = null)
        {
            var details = await _patientService.GetPatientFullDetailsAsync(id, appointmentId);
            if (details == null)
            {
                return NotFound(new { message = "Patient not found or has been deleted." });
            }
            return Ok(details);
        }

        // 5. Patient Profile Creation
        // Requirement: "Doctors can approve the appointment request of the Patient but he cannot create new patient, So Do Nurses."
        // Only Patients themselves create their initial profile during/after registration
        [HttpPost]
        public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.Equals(userRole, "Doctor", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(userRole, "Nurse", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Per system policy, Doctors and Nurses cannot create new patients. Patients must register themselves." });
            }

            var creator = User.Identity?.Name ?? dto.Email;
            var created = await _patientService.CreatePatientAsync(dto, creator);
            return CreatedAtAction(nameof(GetPatientById), new { id = created.PatientID }, created);
        }

        // 6. Update patient details (Patient editing their own details or Doctor/Nurse updating records)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] UpdatePatientDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updater = User.Identity?.Name ?? "System";
            var updated = await _patientService.UpdatePatientAsync(id, dto, updater);
            if (updated == null)
            {
                return NotFound(new { message = "Patient not found or has been deleted." });
            }

            return Ok(updated);
        }

        // 7. Soft Delete Patient (Doctor acting as Admin)
        // Requirement: "whenever a patient is deleted it will not be hard deleted but soft deleted."
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != null && !string.Equals(userRole, "Doctor", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only Doctor (Admin) has permission to delete patient records." });
            }

            var deletedBy = User.Identity?.Name ?? "Doctor Admin";
            var success = await _patientService.SoftDeletePatientAsync(id, deletedBy);
            if (!success)
            {
                return NotFound(new { message = "Patient not found or could not be deleted." });
            }

            return Ok(new { message = "Patient successfully soft-deleted." });
        }

        // 8. Add Vitals for Patient (Recorded by Nurse or Doctor)
        [HttpPost("{id}/vitals")]
        public async Task<IActionResult> AddVital(int id, [FromBody] PatientVitalDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            dto.PatientId = id;
            var recorder = User.Identity?.Name ?? "Healthcare Staff";
            var result = await _patientService.AddVitalAsync(dto, recorder);
            return Ok(result);
        }

        // 9. Add Medical History for Patient (Only Nurse and Patient/User can add; Doctor is View-only)
        [HttpPost("{id}/medical-history")]
        public async Task<IActionResult> AddMedicalHistory(int id, [FromBody] PatientMedicalHistoryDto dto)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (!string.IsNullOrEmpty(userRole) && string.Equals(userRole, "Doctor", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Doctors have view-only access for medical history records. Only Nurses and Patients can log entries." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            dto.PatientId = id;
            var result = await _patientService.AddMedicalHistoryAsync(dto);
            return Ok(result);
        }
    }
}
