using Microsoft.AspNetCore.Mvc;
using AuthService.DTOs;
using AuthService.Services;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(request);
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, message, user) = await _authService.RegisterAsync(request);
            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new
            {
                message,
                userId = user!.UserId,
                email = user.Email,
                fullName = user.FullName,
                role = user.Role?.RoleName
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ForgotPasswordAsync(request);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            // Return OTP in payload so Angular UI can display it in an alert pop-up (as required)
            return Ok(result);
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, message) = await _authService.VerifyOtpAsync(request.Email, request.Otp);
            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { success = true, message });
        }

        [HttpPost("verify-reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, message) = await _authService.ResetPasswordAsync(request);
            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { message });
        }

        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors()
        {
            var doctors = await _authService.GetDoctorsAsync();
            return Ok(doctors);
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _authService.GetDepartmentsAsync();
            return Ok(departments);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                userId = user.UserId,
                fullName = user.FullName,
                email = user.Email,
                role = user.Role?.RoleName,
                phoneNumber = user.PhoneNumber,
                departmentId = user.DepartmentId,
                departmentName = user.Department?.DepartmentName
            });
        }
    }
}
