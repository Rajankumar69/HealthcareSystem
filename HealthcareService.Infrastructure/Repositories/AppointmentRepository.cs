using Microsoft.EntityFrameworkCore;
using HealthcareService.Data;
using HealthcareService.Models;

namespace HealthcareService.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly HealthcareDbContext _context;

        public AppointmentRepository(HealthcareDbContext context)
        {
            _context = context;
        }

        public async Task<List<Appointment_Raj>> GetAppointmentsAsync(int? doctorId, int? patientId, string? status)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .AsQueryable();

            if (doctorId.HasValue)
            {
                query = query.Where(a => a.DoctorId == doctorId.Value);

                // Requirement: New appointment requests go to Nurses first, never directly to Doctors.
                // Doctors only receive appointments after nurses review and forward them.
                if (string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(a => a.Status.ToLower() != "requested");
                }
                else if (status.Trim().ToLower() == "requested")
                {
                    query = query.Where(a => false);
                }
            }

            if (patientId.HasValue)
            {
                query = query.Where(a => a.PatientId == patientId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status.ToLower() == status.Trim().ToLower());
            }

            return await query
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();
        }

        public async Task<Appointment_Raj?> GetAppointmentByIdAsync(int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
        }

        public async Task<Appointment_Raj> CreateAppointmentAsync(Appointment_Raj appointment)
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<bool> UpdateStatusAsync(int appointmentId, string status, string? notes)
        {
            var appt = await _context.Appointments.FindAsync(appointmentId);
            if (appt == null) return false;

            appt.Status = status;
            if (!string.IsNullOrWhiteSpace(notes))
            {
                appt.Notes = notes;
            }
            appt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RescheduleAppointmentAsync(int appointmentId, DateTime newDate, string newTime, string? reason)
        {
            var appt = await _context.Appointments.FindAsync(appointmentId);
            if (appt == null) return false;

            appt.AppointmentDate = newDate;
            appt.AppointmentTime = newTime;
            appt.Status = "Rescheduled";
            if (!string.IsNullOrWhiteSpace(reason))
            {
                appt.Notes = $"Rescheduled: {reason}";
            }
            appt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
