using HealthcareService.DTOs;
using HealthcareService.Models;
using HealthcareService.Repositories;

namespace HealthcareService.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IAppointmentRepository _appointmentRepository;

        public PatientService(IPatientRepository patientRepository, IAppointmentRepository appointmentRepository)
        {
            _patientRepository = patientRepository;
            _appointmentRepository = appointmentRepository;
        }

        public async Task<PaginatedPatientsDto> GetPaginatedPatientsAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            return await _patientRepository.GetPaginatedPatientsAsync(pageNumber, pageSize, searchTerm);
        }

        public async Task<PatientDto?> GetPatientByIdAsync(int patientId)
        {
            var p = await _patientRepository.GetPatientByIdAsync(patientId);
            return p == null ? null : MapToDto(p);
        }

        public async Task<PatientDto?> GetPatientByUserIdAsync(int userId)
        {
            var p = await _patientRepository.GetPatientByUserIdAsync(userId);
            return p == null ? null : MapToDto(p);
        }

        public async Task<PatientFullDetailsDto?> GetPatientFullDetailsAsync(int patientId, int? appointmentId = null)
        {
            var details = await _patientRepository.GetPatientFullDetailsUsingSpAsync(patientId);
            if (details?.PatientDetails == null) return details;

            // 1. If a specific appointmentId is provided, sync its reason as the active chief problem
            if (appointmentId.HasValue && appointmentId.Value > 0)
            {
                var appt = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId.Value);
                if (appt != null && !string.IsNullOrWhiteSpace(appt.Reason))
                {
                    details.PatientDetails.Problem = appt.Reason;
                }
            }
            else
            {
                // 2. Otherwise, check the latest appointment for this patient so the problem is always up-to-date
                var appts = await _appointmentRepository.GetAppointmentsAsync(null, patientId, null);
                var latestAppt = appts.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.AppointmentId).FirstOrDefault();
                if (latestAppt != null && !string.IsNullOrWhiteSpace(latestAppt.Reason))
                {
                    details.PatientDetails.Problem = latestAppt.Reason;
                }
            }

            return details;
        }

        public async Task<PatientDto> CreatePatientAsync(CreatePatientDto dto, string? createdBy)
        {
            var patient = new Patient_Raj
            {
                UserId = dto.UserId,
                Prefix = dto.Prefix,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Gender = dto.Gender,
                PhoneNumber = dto.PhoneNumber.Trim(),
                Email = dto.Email.Trim().ToLower(),
                AnyMedicalHistory = dto.AnyMedicalHistory,
                Problem = dto.Problem,
                AdharNumber = dto.AdharNumber?.Trim(),
                DateOfBirth = dto.DateOfBirth,
                Age = dto.Age,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            var created = await _patientRepository.CreatePatientAsync(patient);

            if (!string.IsNullOrWhiteSpace(dto.AnyMedicalHistory))
            {
                await _patientRepository.AddMedicalHistoryAsync(new PatientMedicalHistory_Raj
                {
                    PatientId = created.PatientID,
                    Condition = dto.AnyMedicalHistory.Trim(),
                    DiagnosisDate = DateTime.UtcNow,
                    Treatment = "Initial registration record",
                    Notes = "Documented during patient profile creation",
                    RecordedDate = DateTime.UtcNow
                });
            }

            return MapToDto(created);
        }

        public async Task<PatientDto?> UpdatePatientAsync(int patientId, UpdatePatientDto dto, string? updatedBy)
        {
            var updated = await _patientRepository.UpdatePatientAsync(patientId, dto, updatedBy);
            if (updated != null && !string.IsNullOrWhiteSpace(dto.AnyMedicalHistory))
            {
                var fullDetails = await _patientRepository.GetPatientFullDetailsUsingSpAsync(patientId);
                bool alreadyExists = fullDetails?.PatientMedicalHistory?.Any(h =>
                    string.Equals(h.Condition, dto.AnyMedicalHistory.Trim(), StringComparison.OrdinalIgnoreCase)) ?? false;

                if (!alreadyExists)
                {
                    await _patientRepository.AddMedicalHistoryAsync(new PatientMedicalHistory_Raj
                    {
                        PatientId = patientId,
                        Condition = dto.AnyMedicalHistory.Trim(),
                        DiagnosisDate = DateTime.UtcNow,
                        Treatment = "Patient profile update",
                        Notes = $"Updated by {updatedBy ?? "Patient"}",
                        RecordedDate = DateTime.UtcNow
                    });
                }
            }
            return updated == null ? null : MapToDto(updated);
        }

        public async Task<bool> SoftDeletePatientAsync(int patientId, string? deletedBy)
        {
            return await _patientRepository.SoftDeletePatientUsingSpAsync(patientId, deletedBy);
        }

        public async Task<PatientVitalDto> AddVitalAsync(PatientVitalDto dto, string? recordedBy)
        {
            var vital = new PatientVital_Raj
            {
                PatientId = dto.PatientId,
                BloodPressure = dto.BloodPressure,
                HeartRate = dto.HeartRate,
                Temperature = dto.Temperature,
                Weight = dto.Weight,
                BloodGroup = dto.BloodGroup,
                RecordedDate = DateTime.UtcNow,
                RecordedBy = recordedBy ?? dto.RecordedBy
            };

            var created = await _patientRepository.AddVitalAsync(vital);
            return new PatientVitalDto
            {
                VitalId = created.VitalId,
                PatientId = created.PatientId,
                BloodPressure = created.BloodPressure,
                HeartRate = created.HeartRate,
                Temperature = created.Temperature,
                Weight = created.Weight,
                BloodGroup = created.BloodGroup,
                RecordedDate = created.RecordedDate,
                RecordedBy = created.RecordedBy
            };
        }

        public async Task<PatientMedicalHistoryDto> AddMedicalHistoryAsync(PatientMedicalHistoryDto dto)
        {
            var history = new PatientMedicalHistory_Raj
            {
                PatientId = dto.PatientId,
                Condition = dto.Condition,
                DiagnosisDate = dto.DiagnosisDate,
                Treatment = dto.Treatment,
                Notes = dto.Notes,
                RecordedDate = DateTime.UtcNow
            };

            var created = await _patientRepository.AddMedicalHistoryAsync(history);
            return new PatientMedicalHistoryDto
            {
                PatientId = created.PatientId,
                Condition = created.Condition,
                DiagnosisDate = created.DiagnosisDate,
                Treatment = created.Treatment,
                Notes = created.Notes,
                RecordedDate = created.RecordedDate
            };
        }

        private static PatientDto MapToDto(Patient_Raj p)
        {
            return new PatientDto
            {
                PatientID = p.PatientID,
                UserId = p.UserId,
                Prefix = p.Prefix,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Gender = p.Gender,
                PhoneNumber = p.PhoneNumber,
                Email = p.Email,
                AnyMedicalHistory = p.AnyMedicalHistory,
                Problem = p.Problem,
                AdharNumber = p.AdharNumber,
                DateOfBirth = p.DateOfBirth,
                Age = p.Age,
                IsDeleted = p.IsDeleted,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                DeletedBy = p.DeletedBy,
                DeletedDate = p.DeletedDate
            };
        }
    }
}
