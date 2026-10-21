using HealthcareService.DTOs;
using HealthcareService.Models;
using HealthcareService.Repositories;

namespace HealthcareService.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IAuthServiceClient _authServiceClient;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IPatientRepository patientRepository,
            IAuthServiceClient authServiceClient)
        {
            _appointmentRepository = appointmentRepository;
            _patientRepository = patientRepository;
            _authServiceClient = authServiceClient;
        }

        public async Task<List<AppointmentDto>> GetAppointmentsAsync(int? doctorId, int? patientId, string? status)
        {
            var list = await _appointmentRepository.GetAppointmentsAsync(doctorId, patientId, status);
            var result = new List<AppointmentDto>();

            // Cache doctor names to minimize HTTP calls
            var doctorCache = new Dictionary<int, string>();

            foreach (var item in list)
            {
                if (!doctorCache.TryGetValue(item.DoctorId, out var docName))
                {
                    var docUser = await _authServiceClient.GetUserByIdAsync(item.DoctorId);
                    docName = docUser?.FullName ?? $"Doctor #{item.DoctorId}";
                    doctorCache[item.DoctorId] = docName;
                }

                result.Add(new AppointmentDto
                {
                    AppointmentId = item.AppointmentId,
                    PatientId = item.PatientId,
                    PatientName = item.Patient != null ? $"{item.Patient.Prefix} {item.Patient.FirstName} {item.Patient.LastName}".Trim() : $"Patient #{item.PatientId}",
                    PatientPhone = item.Patient?.PhoneNumber ?? string.Empty,
                    PatientEmail = item.Patient?.Email ?? string.Empty,
                    DoctorId = item.DoctorId,
                    DoctorName = docName,
                    AppointmentDate = item.AppointmentDate,
                    AppointmentTime = item.AppointmentTime,
                    Reason = item.Reason,
                    Status = item.Status,
                    Notes = item.Notes,
                    CreatedAt = item.CreatedAt
                });
            }

            return result;
        }

        public async Task<AppointmentDto?> GetAppointmentByIdAsync(int appointmentId)
        {
            var item = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (item == null) return null;

            var docUser = await _authServiceClient.GetUserByIdAsync(item.DoctorId);
            var docName = docUser?.FullName ?? $"Doctor #{item.DoctorId}";

            return new AppointmentDto
            {
                AppointmentId = item.AppointmentId,
                PatientId = item.PatientId,
                PatientName = item.Patient != null ? $"{item.Patient.Prefix} {item.Patient.FirstName} {item.Patient.LastName}".Trim() : $"Patient #{item.PatientId}",
                PatientPhone = item.Patient?.PhoneNumber ?? string.Empty,
                PatientEmail = item.Patient?.Email ?? string.Empty,
                DoctorId = item.DoctorId,
                DoctorName = docName,
                AppointmentDate = item.AppointmentDate,
                AppointmentTime = item.AppointmentTime,
                Reason = item.Reason,
                Status = item.Status,
                Notes = item.Notes,
                CreatedAt = item.CreatedAt
            };
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto)
        {
            var appointment = new Appointment_Raj
            {
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                AppointmentDate = dto.AppointmentDate,
                AppointmentTime = dto.AppointmentTime,
                Reason = dto.Reason,
                Status = "Requested", // Initial status from patient
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _appointmentRepository.CreateAppointmentAsync(appointment);

            // 1. If patient provided medical history with this appointment booking:
            if (!string.IsNullOrWhiteSpace(dto.MedicalHistory))
            {
                var cond = dto.MedicalHistory.Trim();
                var trt = !string.IsNullOrWhiteSpace(dto.Treatment) ? dto.Treatment.Trim() : "Patient self-reported";
                var notes = $"Logged during consultation booking for {dto.AppointmentDate:yyyy-MM-dd}. Reason: {dto.Reason}";

                // Insert into PatientMedicalHistories_Raj table
                await _patientRepository.AddMedicalHistoryAsync(new PatientMedicalHistory_Raj
                {
                    PatientId = dto.PatientId,
                    Condition = cond,
                    DiagnosisDate = dto.AppointmentDate,
                    Treatment = trt,
                    Notes = notes,
                    RecordedDate = DateTime.UtcNow
                });
            }

            // 2. ALWAYS sync the appointment's Reason & Medical History into Patients_Raj table
            var existingPatient = await _patientRepository.GetPatientByIdAsync(dto.PatientId);
            if (existingPatient != null)
            {
                bool modified = false;

                if (!string.IsNullOrWhiteSpace(dto.Reason) && existingPatient.Problem != dto.Reason.Trim())
                {
                    existingPatient.Problem = dto.Reason.Trim();
                    modified = true;
                }

                if (!string.IsNullOrWhiteSpace(dto.MedicalHistory))
                {
                    var cond = dto.MedicalHistory.Trim();
                    if (string.IsNullOrWhiteSpace(existingPatient.AnyMedicalHistory))
                    {
                        existingPatient.AnyMedicalHistory = cond;
                        modified = true;
                    }
                    else if (!existingPatient.AnyMedicalHistory.Contains(cond, StringComparison.OrdinalIgnoreCase))
                    {
                        existingPatient.AnyMedicalHistory += $"; {cond}";
                        modified = true;
                    }
                }

                if (modified)
                {
                    var updateDto = new UpdatePatientDto
                    {
                        Prefix = existingPatient.Prefix,
                        FirstName = existingPatient.FirstName,
                        LastName = existingPatient.LastName,
                        Gender = existingPatient.Gender ?? "Male",
                        PhoneNumber = existingPatient.PhoneNumber,
                        Email = existingPatient.Email,
                        AnyMedicalHistory = existingPatient.AnyMedicalHistory,
                        Problem = existingPatient.Problem,
                        AdharNumber = existingPatient.AdharNumber ?? string.Empty,
                        DateOfBirth = existingPatient.DateOfBirth,
                        Age = existingPatient.Age
                    };
                    await _patientRepository.UpdatePatientAsync(existingPatient.PatientID, updateDto, "Patient Booking");
                }
            }

            var patient = await _patientRepository.GetPatientByIdAsync(dto.PatientId);
            var docUser = await _authServiceClient.GetUserByIdAsync(dto.DoctorId);

            return new AppointmentDto
            {
                AppointmentId = created.AppointmentId,
                PatientId = created.PatientId,
                PatientName = patient != null ? $"{patient.Prefix} {patient.FirstName} {patient.LastName}".Trim() : $"Patient #{dto.PatientId}",
                PatientPhone = patient?.PhoneNumber ?? string.Empty,
                PatientEmail = patient?.Email ?? string.Empty,
                DoctorId = created.DoctorId,
                DoctorName = docUser?.FullName ?? $"Doctor #{dto.DoctorId}",
                AppointmentDate = created.AppointmentDate,
                AppointmentTime = created.AppointmentTime,
                Reason = created.Reason,
                Status = created.Status,
                Notes = created.Notes,
                CreatedAt = created.CreatedAt
            };
        }

        public async Task<bool> NurseApproveAsync(int appointmentId, string? notes)
        {
            // Status transitions from Requested -> NurseApproved
            return await _appointmentRepository.UpdateStatusAsync(appointmentId, "NurseApproved", notes);
        }

        public async Task<bool> DoctorConfirmAsync(int appointmentId, string? notes)
        {
            var appt = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (appt == null) return false;

            // Workflow enforcement: Doctor can confirm appointments that have been reviewed by a Nurse (NurseApproved) OR Rescheduled by Nurse
            if (!string.Equals(appt.Status, "NurseApproved", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(appt.Status, "Rescheduled", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Status transitions to DoctorConfirmed
            return await _appointmentRepository.UpdateStatusAsync(appointmentId, "DoctorConfirmed", notes);
        }

        public async Task<bool> CancelAppointmentAsync(int appointmentId, string? notes)
        {
            var appt = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (appt == null) return false;

            return await _appointmentRepository.UpdateStatusAsync(appointmentId, "Cancelled", notes ?? "Cancelled by Doctor");
        }

        public async Task<bool> RescheduleAsync(int appointmentId, RescheduleAppointmentDto dto)
        {
            return await _appointmentRepository.RescheduleAppointmentAsync(
                appointmentId,
                dto.NewDate,
                dto.NewTime,
                dto.Reason
            );
        }
    }
}
