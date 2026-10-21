using HealthcareService.DTOs;
using HealthcareService.Models;

namespace HealthcareService.Repositories
{
    public interface IPatientRepository
    {
        Task<PaginatedPatientsDto> GetPaginatedPatientsAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<Patient_Raj?> GetPatientByIdAsync(int patientId);
        Task<Patient_Raj?> GetPatientByUserIdAsync(int userId);
        Task<PatientFullDetailsDto?> GetPatientFullDetailsUsingSpAsync(int patientId);
        Task<Patient_Raj> CreatePatientAsync(Patient_Raj patient);
        Task<Patient_Raj?> UpdatePatientAsync(int patientId, UpdatePatientDto dto, string? updatedBy);
        Task<bool> SoftDeletePatientUsingSpAsync(int patientId, string? deletedBy);
        Task<PatientVital_Raj> AddVitalAsync(PatientVital_Raj vital);
        Task<PatientMedicalHistory_Raj> AddMedicalHistoryAsync(PatientMedicalHistory_Raj history);
    }
}
