using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using HealthcareService.Data;
using HealthcareService.DTOs;
using HealthcareService.Models;

namespace HealthcareService.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly HealthcareDbContext _context;

        public PatientRepository(HealthcareDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedPatientsDto> GetPaginatedPatientsAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var result = new PaginatedPatientsDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = new List<PatientDto>()
            };

            var connection = _context.Database.GetDbConnection();
            bool wasClosed = connection.State == ConnectionState.Closed;
            if (wasClosed) await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "sp_GetPaginatedPatients_Raj";
                command.CommandType = CommandType.StoredProcedure;

                var pPageNumber = command.CreateParameter();
                pPageNumber.ParameterName = "@PageNumber";
                pPageNumber.Value = pageNumber;
                command.Parameters.Add(pPageNumber);

                var pPageSize = command.CreateParameter();
                pPageSize.ParameterName = "@PageSize";
                pPageSize.Value = pageSize;
                command.Parameters.Add(pPageSize);

                var pSearchTerm = command.CreateParameter();
                pSearchTerm.ParameterName = "@SearchTerm";
                pSearchTerm.Value = string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm.Trim();
                command.Parameters.Add(pSearchTerm);

                var pTotalCount = command.CreateParameter();
                pTotalCount.ParameterName = "@TotalCount";
                pTotalCount.Direction = ParameterDirection.Output;
                pTotalCount.DbType = DbType.Int32;
                command.Parameters.Add(pTotalCount);

                int totalRecordsFromRow = 0;
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    try { totalRecordsFromRow = reader.GetInt32(reader.GetOrdinal("TotalRecords")); } catch { }
                    result.Items.Add(new PatientDto
                    {
                        PatientID = reader.GetInt32(reader.GetOrdinal("PatientID")),
                        UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? 0 : reader.GetInt32(reader.GetOrdinal("UserId")),
                        Prefix = reader.IsDBNull(reader.GetOrdinal("Prefix")) ? "Mr." : reader.GetString(reader.GetOrdinal("Prefix")),
                        FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "" : reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? "" : reader.GetString(reader.GetOrdinal("LastName")),
                        Gender = GetSafeString(reader, "Gender"),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? "" : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString(reader.GetOrdinal("Email")),
                        AnyMedicalHistory = reader.IsDBNull(reader.GetOrdinal("AnyMedicalHistory")) ? null : reader.GetString(reader.GetOrdinal("AnyMedicalHistory")),
                        Problem = reader.IsDBNull(reader.GetOrdinal("Problem")) ? null : reader.GetString(reader.GetOrdinal("Problem")),
                        AdharNumber = reader.IsDBNull(reader.GetOrdinal("AdharNumber")) ? "" : reader.GetString(reader.GetOrdinal("AdharNumber")),
                        DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                        Age = reader.IsDBNull(reader.GetOrdinal("Age")) ? 0 : reader.GetInt32(reader.GetOrdinal("Age")),
                        IsDeleted = reader.IsDBNull(reader.GetOrdinal("IsDeleted")) ? false : reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                        CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    });
                }
                await reader.CloseAsync();

                if (totalRecordsFromRow > 0)
                {
                    result.TotalCount = totalRecordsFromRow;
                }
                else
                {
                    result.TotalCount = (pTotalCount.Value != null && pTotalCount.Value != DBNull.Value) ? Convert.ToInt32(pTotalCount.Value) : 0;
                }
            }
            finally
            {
                if (wasClosed) await connection.CloseAsync();
            }

            return result;
        }

        public async Task<Patient_Raj?> GetPatientByIdAsync(int patientId)
        {
            return await _context.Patients
                .FirstOrDefaultAsync(p => p.PatientID == patientId && !p.IsDeleted);
        }

        public async Task<Patient_Raj?> GetPatientByUserIdAsync(int userId)
        {
            return await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);
        }

        public async Task<PatientFullDetailsDto?> GetPatientFullDetailsUsingSpAsync(int patientId)
        {
            var result = new PatientFullDetailsDto();

            var connection = _context.Database.GetDbConnection();
            bool wasClosed = connection.State == ConnectionState.Closed;
            if (wasClosed) await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "sp_GetPatientFullDetails_Raj";
                command.CommandType = CommandType.StoredProcedure;

                var pId = command.CreateParameter();
                pId.ParameterName = "@PatientID";
                pId.Value = patientId;
                command.Parameters.Add(pId);

                using var reader = await command.ExecuteReaderAsync();

                // 1. First result set: Patient Details
                if (await reader.ReadAsync())
                {
                    result.PatientDetails = new PatientDto
                    {
                        PatientID = reader.GetInt32(reader.GetOrdinal("PatientID")),
                        UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? 0 : reader.GetInt32(reader.GetOrdinal("UserId")),
                        Prefix = reader.IsDBNull(reader.GetOrdinal("Prefix")) ? "Mr." : reader.GetString(reader.GetOrdinal("Prefix")),
                        FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "" : reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? "" : reader.GetString(reader.GetOrdinal("LastName")),
                        Gender = GetSafeString(reader, "Gender"),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? "" : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString(reader.GetOrdinal("Email")),
                        AnyMedicalHistory = reader.IsDBNull(reader.GetOrdinal("AnyMedicalHistory")) ? null : reader.GetString(reader.GetOrdinal("AnyMedicalHistory")),
                        Problem = reader.IsDBNull(reader.GetOrdinal("Problem")) ? null : reader.GetString(reader.GetOrdinal("Problem")),
                        AdharNumber = reader.IsDBNull(reader.GetOrdinal("AdharNumber")) ? "" : reader.GetString(reader.GetOrdinal("AdharNumber")),
                        DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                        Age = reader.IsDBNull(reader.GetOrdinal("Age")) ? 0 : reader.GetInt32(reader.GetOrdinal("Age")),
                        IsDeleted = reader.IsDBNull(reader.GetOrdinal("IsDeleted")) ? false : reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                        CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    };
                }
                else
                {
                    return null; // Patient not found or deleted
                }

                // 2. Second result set: Patient Vitals
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.PatientVitals.Add(new PatientVitalDto
                        {
                            VitalId = reader.IsDBNull(reader.GetOrdinal("VitalId")) ? null : reader.GetInt32(reader.GetOrdinal("VitalId")),
                            PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                            BloodPressure = reader.IsDBNull(reader.GetOrdinal("BloodPressure")) ? "120/80" : reader.GetString(reader.GetOrdinal("BloodPressure")),
                            HeartRate = reader.IsDBNull(reader.GetOrdinal("HeartRate")) ? 0 : reader.GetInt32(reader.GetOrdinal("HeartRate")),
                            Temperature = reader.IsDBNull(reader.GetOrdinal("Temperature")) ? 98.6m : reader.GetDecimal(reader.GetOrdinal("Temperature")),
                            Weight = reader.IsDBNull(reader.GetOrdinal("Weight")) ? 70.0m : reader.GetDecimal(reader.GetOrdinal("Weight")),
                            BloodGroup = reader.IsDBNull(reader.GetOrdinal("BloodGroup")) ? null : reader.GetString(reader.GetOrdinal("BloodGroup")),
                            RecordedDate = reader.IsDBNull(reader.GetOrdinal("RecordedDate")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("RecordedDate")),
                            RecordedBy = reader.IsDBNull(reader.GetOrdinal("RecordedBy")) ? null : reader.GetString(reader.GetOrdinal("RecordedBy"))
                        });
                    }
                }

                // 3. Third result set: Patient Medical History
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.PatientMedicalHistory.Add(new PatientMedicalHistoryDto
                        {
                            PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                            Condition = reader.IsDBNull(reader.GetOrdinal("Condition")) ? "General Condition" : reader.GetString(reader.GetOrdinal("Condition")),
                            DiagnosisDate = reader.IsDBNull(reader.GetOrdinal("DiagnosisDate")) ? null : reader.GetDateTime(reader.GetOrdinal("DiagnosisDate")),
                            Treatment = reader.IsDBNull(reader.GetOrdinal("Treatment")) ? null : reader.GetString(reader.GetOrdinal("Treatment")),
                            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                            RecordedDate = reader.IsDBNull(reader.GetOrdinal("RecordedDate")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("RecordedDate"))
                        });
                    }
                }
            }
            finally
            {
                if (wasClosed) await connection.CloseAsync();
            }

            return result;
        }

        public async Task<Patient_Raj> CreatePatientAsync(Patient_Raj patient)
        {
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task<Patient_Raj?> UpdatePatientAsync(int patientId, UpdatePatientDto dto, string? updatedBy)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientID == patientId && !p.IsDeleted);
            if (patient == null) return null;

            patient.Prefix = dto.Prefix;
            patient.FirstName = dto.FirstName;
            patient.LastName = dto.LastName;
            patient.Gender = dto.Gender;
            patient.PhoneNumber = dto.PhoneNumber;
            patient.Email = dto.Email;
            patient.AnyMedicalHistory = dto.AnyMedicalHistory;
            patient.Problem = dto.Problem;
            patient.AdharNumber = dto.AdharNumber;
            patient.DateOfBirth = dto.DateOfBirth;
            patient.Age = dto.Age;
            patient.UpdatedAt = DateTime.UtcNow;
            patient.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task<bool> SoftDeletePatientUsingSpAsync(int patientId, string? deletedBy)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientID == patientId && !p.IsDeleted);
            if (patient == null) return false;

            patient.IsDeleted = true;
            patient.DeletedBy = deletedBy ?? "Doctor Admin";
            patient.DeletedDate = DateTime.UtcNow;
            patient.UpdatedAt = DateTime.UtcNow;
            patient.UpdatedBy = deletedBy ?? "Doctor Admin";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PatientVital_Raj> AddVitalAsync(PatientVital_Raj vital)
        {
            var existing = await _context.Vitals.FirstOrDefaultAsync(v => v.PatientId == vital.PatientId);
            if (existing != null)
            {
                existing.BloodPressure = vital.BloodPressure;
                existing.HeartRate = vital.HeartRate;
                existing.Temperature = vital.Temperature;
                existing.Weight = vital.Weight;
                if (!string.IsNullOrEmpty(vital.BloodGroup))
                {
                    existing.BloodGroup = vital.BloodGroup;
                }
                existing.RecordedDate = DateTime.UtcNow;
                existing.RecordedBy = vital.RecordedBy;
            }
            else
            {
                _context.Vitals.Add(vital);
            }
            await _context.SaveChangesAsync();
            return existing ?? vital;
        }

        public async Task<PatientMedicalHistory_Raj> AddMedicalHistoryAsync(PatientMedicalHistory_Raj history)
        {
            var existing = await _context.MedicalHistories.FindAsync(history.PatientId);
            if (existing != null)
            {
                existing.Condition = history.Condition;
                existing.DiagnosisDate = history.DiagnosisDate;
                existing.Treatment = history.Treatment;
                existing.Notes = history.Notes;
                existing.RecordedDate = DateTime.UtcNow;
            }
            else
            {
                _context.MedicalHistories.Add(history);
            }
            await _context.SaveChangesAsync();
            return existing ?? history;
        }

        private static string? GetSafeString(System.Data.Common.DbDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch
            {
                return null;
            }
        }
    }
}
