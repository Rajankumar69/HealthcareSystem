namespace AuthService.Repositories
{
    public interface IOtpRepository
    {
        Task<(bool Success, string Message)> SaveOtpUsingSpAsync(string email, string otp);
        Task<(bool Success, string Message)> VerifyOtpUsingSpAsync(string email, string otp);
        Task<(bool Success, string Message)> VerifyOtpAndResetPasswordUsingSpAsync(string email, string otp, string newPasswordHash);
    }
}
