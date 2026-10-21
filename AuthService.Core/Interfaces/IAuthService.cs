using AuthService.DTOs;
using AuthService.Models;

namespace AuthService.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<(bool Success, string Message, User_Raj? User)> RegisterAsync(RegisterRequestDto request);
        Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otp);
        Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<List<DoctorInfoDto>> GetDoctorsAsync();
        Task<List<DepartmentDto>> GetDepartmentsAsync();
        Task<User_Raj?> GetUserByIdAsync(int id);
    }
}
