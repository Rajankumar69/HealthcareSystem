using HealthcareService.DTOs;

namespace HealthcareService.Services
{
    public interface IAppointmentService
    {
        Task<List<AppointmentDto>> GetAppointmentsAsync(int? doctorId, int? patientId, string? status);
        Task<AppointmentDto?> GetAppointmentByIdAsync(int appointmentId);
        Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto);
        Task<bool> NurseApproveAsync(int appointmentId, string? notes);
        Task<bool> DoctorConfirmAsync(int appointmentId, string? notes);
        Task<bool> CancelAppointmentAsync(int appointmentId, string? notes);
        Task<bool> RescheduleAsync(int appointmentId, RescheduleAppointmentDto dto);
    }
}
