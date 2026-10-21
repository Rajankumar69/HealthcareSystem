using System.ComponentModel.DataAnnotations;

namespace HealthcareService.DTOs
{
    public class AppointmentDto
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string AppointmentTime { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty; // Requested, NurseApproved, DoctorConfirmed, Rescheduled, Cancelled
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateAppointmentDto
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public string AppointmentTime { get; set; } = string.Empty;

        public string? Reason { get; set; }
        public string? MedicalHistory { get; set; }
        public string? Treatment { get; set; }
        public string? Notes { get; set; }
    }

    public class RescheduleAppointmentDto
    {
        [Required]
        public DateTime NewDate { get; set; }

        [Required]
        public string NewTime { get; set; } = string.Empty;

        public string? Reason { get; set; }
    }
}
