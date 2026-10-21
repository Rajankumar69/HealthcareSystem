using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareService.Models
{
    [Table("PatientMedicalHistories_Raj")]
    public class PatientMedicalHistory_Raj
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public virtual Patient_Raj? Patient { get; set; }

        [Required]
        [MaxLength(200)]
        public string Condition { get; set; } = string.Empty;

        public DateTime? DiagnosisDate { get; set; }

        public string? Treatment { get; set; }

        public string? Notes { get; set; }

        public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
    }
}
