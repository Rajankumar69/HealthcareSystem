using HealthcareService.DTOs;
using HealthcareService.Models;

namespace HealthcareService.Repositories
{
    public interface IAppointmentRepository
    {
        Task<List<Appointment_Raj>> GetAppointmentsAsync(int? doctorId, int? patientId, string? status);
        Task<Appointment_Raj?> GetAppointmentByIdAsync(int appointmentId);
        Task<Appointment_Raj> CreateAppointmentAsync(Appointment_Raj appointment);
        Task<bool> UpdateStatusAsync(int appointmentId, string status, string? notes);
        Task<bool> RescheduleAppointmentAsync(int appointmentId, DateTime newDate, string newTime, string? reason);
    }
}
