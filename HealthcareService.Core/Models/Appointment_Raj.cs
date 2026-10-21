using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareService.Models
{
    [Table("Appointments_Raj")]
    public class Appointment_Raj
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public virtual Patient_Raj? Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string AppointmentTime { get; set; } = string.Empty;

        public string? Reason { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Requested"; // Requested, NurseApproved, DoctorConfirmed, Rescheduled, Cancelled

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
