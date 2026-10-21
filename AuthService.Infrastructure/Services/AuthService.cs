using System.Security.Cryptography;
using System.Text;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;

namespace AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpRepository _otpRepository;
        private readonly ITokenService _tokenService;

        public AuthService(
            IUserRepository userRepository,
            IOtpRepository otpRepository,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _otpRepository = otpRepository;
            _tokenService = tokenService;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            var hash = ComputeSha256Hash(request.Password);
            if (user.PasswordHash != hash)
            {
                return null;
            }

            var roleName = user.Role?.RoleName ?? "Patient";
            var token = _tokenService.GenerateToken(user, roleName);

            return new LoginResponseDto
            {
                Token = token,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = roleName,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.DepartmentName,
                Expiration = DateTime.UtcNow.AddHours(8)
            };
        }

        public async Task<(bool Success, string Message, User_Raj? User)> RegisterAsync(RegisterRequestDto request)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return (false, "An account with this email address already exists.", null);
            }

            // Requirement: New registration will always be the patient
            var role = await _userRepository.GetRoleByNameAsync("Patient");
            if (role == null)
            {
                return (false, "Patient role is not configured in the system.", null);
            }

            var user = new User_Raj
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim().ToLower(),
                PasswordHash = ComputeSha256Hash(request.Password),
                RoleId = role.RoleId,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.AddUserAsync(user);
            createdUser.Role = role;

            return (true, "Registration successful.", createdUser);
        }

        public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "No user found with the provided email address."
                };
            }

            // Generate 6-digit secure OTP
            var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // Save OTP in PasswordResets_Raj using Stored Procedure with 30-sec expiry
            var (success, message) = await _otpRepository.SaveOtpUsingSpAsync(user.Email, otp);

            if (!success)
            {
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = message
                };
            }

            return new ForgotPasswordResponseDto
            {
                Success = true,
                Message = "OTP generated successfully! Please enter it within 30 seconds.",
                GeneratedOtp = otp,
                ValiditySeconds = 30
            };
        }

        public async Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otp)
        {
            return await _otpRepository.VerifyOtpUsingSpAsync(email, otp);
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                return (false, "Passwords do not match.");
            }

            var newPasswordHash = ComputeSha256Hash(request.NewPassword);

            // Verify OTP and update password in Users_Raj using Stored Procedure
            return await _otpRepository.VerifyOtpAndResetPasswordUsingSpAsync(
                request.Email,
                request.Otp,
                newPasswordHash
            );
        }

        public async Task<List<DoctorInfoDto>> GetDoctorsAsync()
        {
            var doctors = await _userRepository.GetUsersByRoleAsync("Doctor");
            return doctors.Select(d => new DoctorInfoDto
            {
                UserId = d.UserId,
                FullName = d.FullName,
                Email = d.Email,
                PhoneNumber = d.PhoneNumber,
                DepartmentId = d.DepartmentId,
                DepartmentName = d.Department?.DepartmentName
            }).ToList();
        }

        public async Task<List<DepartmentDto>> GetDepartmentsAsync()
        {
            var departments = await _userRepository.GetDepartmentsAsync();
            return departments.Select(d => new DepartmentDto
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,
                Description = d.Description,
                IsActive = d.IsActive
            }).ToList();
        }

        public async Task<User_Raj?> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public static string ComputeSha256Hash(string rawData)
        {
            using var sha256Hash = SHA256.Create();
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
