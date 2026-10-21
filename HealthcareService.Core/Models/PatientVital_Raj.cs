using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareService.Models
{
    [Table("PatientVitals_Raj")]
    public class PatientVital_Raj
    {
        [Key]
        public int VitalId { get; set; }

        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public virtual Patient_Raj? Patient { get; set; }

        [Required]
        [MaxLength(50)]
        public string BloodPressure { get; set; } = string.Empty;

        [Required]
        public int HeartRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Temperature { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Weight { get; set; }

        [MaxLength(10)]
        public string? BloodGroup { get; set; }

        public DateTime RecordedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? RecordedBy { get; set; }
    }
}
