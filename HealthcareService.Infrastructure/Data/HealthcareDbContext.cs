using Microsoft.EntityFrameworkCore;
using HealthcareService.Models;

namespace HealthcareService.Data
{
    public class HealthcareDbContext : DbContext
    {
        public HealthcareDbContext(DbContextOptions<HealthcareDbContext> options) : base(options)
        {
        }

        public DbSet<Patient_Raj> Patients { get; set; }
        public DbSet<Appointment_Raj> Appointments { get; set; }
        public DbSet<PatientVital_Raj> Vitals { get; set; }
        public DbSet<PatientMedicalHistory_Raj> MedicalHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Patient_Raj>().ToTable("Patients_Raj");
            modelBuilder.Entity<Appointment_Raj>().ToTable("Appointments_Raj");
            modelBuilder.Entity<PatientVital_Raj>()
                .ToTable("PatientVitals_Raj")
                .HasKey(v => v.VitalId);
            modelBuilder.Entity<PatientMedicalHistory_Raj>()
                .ToTable("PatientMedicalHistories_Raj")
                .HasKey(m => m.PatientId);

            // Soft-delete query filter
            modelBuilder.Entity<Patient_Raj>()
                .HasQueryFilter(p => !p.IsDeleted);
        }
    }
}
