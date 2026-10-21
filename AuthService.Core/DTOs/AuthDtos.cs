using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime Expiration { get; set; }
    }

    public class RegisterRequestDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Patient"; // "Patient", "Doctor", "Nurse"

        [Required(ErrorMessage = "Phone number is mandatory.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ForgotPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        // User requirement: "* when user fill his correct email an pop up alert will give him the otp*"
        public string? GeneratedOtp { get; set; }
        public int ValiditySeconds { get; set; } = 30;
    }

    public class VerifyOtpRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Otp { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Otp { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class DoctorInfoDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
    }

    public class DepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    // ==========================================
    // ADMIN DASHBOARD DTOs (Beginner-friendly)
    // ==========================================

    public class AdminDoctorPatientDto
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AppointmentDate { get; set; }
        public string? AppointmentTime { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
    }

    public class AdminDoctorDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public List<AdminDoctorPatientDto> Patients { get; set; } = new();
    }

    public class AdminNursePatientDto
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? BloodPressure { get; set; }
        public int? HeartRate { get; set; }
        public decimal? Temperature { get; set; }
        public string? RecordedDate { get; set; }
    }

    public class AdminNurseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public List<AdminNursePatientDto> Patients { get; set; } = new();
    }

    public class AdminPatientDto
    {
        public int PatientId { get; set; }
        public int? UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public int Age { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Problem { get; set; }
        public string? AnyMedicalHistory { get; set; }
        public bool IsActive { get; set; }
        public string? AssignedDoctor { get; set; }
        public string? AppointmentDate { get; set; }
        public string? AppointmentStatus { get; set; }
    }

    public class AdminUsersResponseDto
    {
        public List<AdminDoctorDto> Doctors { get; set; } = new();
        public List<AdminNurseDto> Nurses { get; set; } = new();
        public List<AdminPatientDto> Patients { get; set; } = new();
    }

    public class AdminCreateUserDto
    {
        [Required]
        public string Role { get; set; } = "Doctor"; // "Doctor", "Nurse", "Patient"

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public int? DepartmentId { get; set; }

        // Patient specific
        public string? Gender { get; set; }
        public int? Age { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Problem { get; set; }
        public string? AnyMedicalHistory { get; set; }
        public string? AdharNumber { get; set; }
    }
}
