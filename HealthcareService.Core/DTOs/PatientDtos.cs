using System.ComponentModel.DataAnnotations;

namespace HealthcareService.DTOs
{
    public class PatientDto
    {
        public int PatientID { get; set; }
        public int UserId { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{Prefix} {FirstName} {LastName}".Trim();
        public string? Gender { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AnyMedicalHistory { get; set; }
        public string? Problem { get; set; }
        public string? AdharNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }

    public class CreatePatientDto
    {
        [Required(ErrorMessage = "UserId is mandatory.")]
        public int UserId { get; set; }

        [Required]
        public string Prefix { get; set; } = "Mr.";

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? Gender { get; set; }

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? AnyMedicalHistory { get; set; }

        public string? Problem { get; set; }

        [Required(ErrorMessage = "Aadhaar number is mandatory.")]
        public string AdharNumber { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Range(0, 150)]
        public int Age { get; set; }
    }

    public class UpdatePatientDto
    {
        [Required]
        public string Prefix { get; set; } = "Mr.";

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is mandatory.")]
        public string Gender { get; set; } = "Male";

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? AnyMedicalHistory { get; set; }

        public string? Problem { get; set; }

        [Required(ErrorMessage = "Aadhaar number is mandatory.")]
        public string AdharNumber { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Range(0, 150)]
        public int Age { get; set; }
    }

    public class PaginatedPatientsDto
    {
        public List<PatientDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class PatientVitalDto
    {
        public int? VitalId { get; set; }
        public int PatientId { get; set; }

        [Required]
        public string BloodPressure { get; set; } = string.Empty;

        [Required]
        public int HeartRate { get; set; }

        [Required]
        public decimal Temperature { get; set; }

        [Required]
        public decimal Weight { get; set; }

        public string? BloodGroup { get; set; }

        public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
        public string? RecordedBy { get; set; }
    }

    public class PatientMedicalHistoryDto
    {
        public int? HistoryId { get; set; }
        public int PatientId { get; set; }

        [Required]
        public string Condition { get; set; } = string.Empty;

        public DateTime? DiagnosisDate { get; set; }
        public string? Treatment { get; set; }
        public string? Notes { get; set; }
        public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
    }

    public class PatientFullDetailsDto
    {
        // Section 1: Patient details
        public PatientDto PatientDetails { get; set; } = new();

        // Section 2: Patient Vitals
        public List<PatientVitalDto> PatientVitals { get; set; } = new();

        // Section 3: Patient Medical History
        public List<PatientMedicalHistoryDto> PatientMedicalHistory { get; set; } = new();
    }
}
