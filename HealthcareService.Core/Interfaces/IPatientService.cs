using HealthcareService.DTOs;

namespace HealthcareService.Services
{
    public interface IPatientService
    {
        Task<PaginatedPatientsDto> GetPaginatedPatientsAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<PatientDto?> GetPatientByIdAsync(int patientId);
        Task<PatientDto?> GetPatientByUserIdAsync(int userId);
        Task<PatientFullDetailsDto?> GetPatientFullDetailsAsync(int patientId, int? appointmentId = null);
        Task<PatientDto> CreatePatientAsync(CreatePatientDto dto, string? createdBy);
        Task<PatientDto?> UpdatePatientAsync(int patientId, UpdatePatientDto dto, string? updatedBy);
        Task<bool> SoftDeletePatientAsync(int patientId, string? deletedBy);
        Task<PatientVitalDto> AddVitalAsync(PatientVitalDto dto, string? recordedBy);
        Task<PatientMedicalHistoryDto> AddMedicalHistoryAsync(PatientMedicalHistoryDto dto);
    }
}
