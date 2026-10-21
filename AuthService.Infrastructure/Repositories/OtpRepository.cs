using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using AuthService.Data;

namespace AuthService.Repositories
{
    public class OtpRepository : IOtpRepository
    {
        private readonly AuthDbContext _context;

        public OtpRepository(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> SaveOtpUsingSpAsync(string email, string otp)
        {
            var emailParam = new SqlParameter("@Email", SqlDbType.NVarChar, 150) { Value = email.Trim() };
            var otpParam = new SqlParameter("@Otp", SqlDbType.NVarChar, 10) { Value = otp.Trim() };
            var successParam = new SqlParameter("@Success", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            var messageParam = new SqlParameter("@Message", SqlDbType.NVarChar, 250) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_SaveOrUpdateOtp_Raj @Email, @Otp, @Success OUTPUT, @Message OUTPUT",
                emailParam, otpParam, successParam, messageParam
            );

            bool success = successParam.Value != DBNull.Value && (bool)successParam.Value;
            string message = messageParam.Value != DBNull.Value ? (string)messageParam.Value : string.Empty;

            return (success, message);
        }

        public async Task<(bool Success, string Message)> VerifyOtpUsingSpAsync(string email, string otp)
        {
            var emailParam = new SqlParameter("@Email", SqlDbType.NVarChar, 150) { Value = email.Trim() };
            var otpParam = new SqlParameter("@Otp", SqlDbType.NVarChar, 10) { Value = otp.Trim() };
            var successParam = new SqlParameter("@Success", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            var messageParam = new SqlParameter("@Message", SqlDbType.NVarChar, 250) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_VerifyOtp_Raj @Email, @Otp, @Success OUTPUT, @Message OUTPUT",
                emailParam, otpParam, successParam, messageParam
            );

            bool success = successParam.Value != DBNull.Value && (bool)successParam.Value;
            string message = messageParam.Value != DBNull.Value ? (string)messageParam.Value : string.Empty;

            return (success, message);
        }

        public async Task<(bool Success, string Message)> VerifyOtpAndResetPasswordUsingSpAsync(string email, string otp, string newPasswordHash)
        {
            var emailParam = new SqlParameter("@Email", SqlDbType.NVarChar, 150) { Value = email.Trim() };
            var otpParam = new SqlParameter("@Otp", SqlDbType.NVarChar, 10) { Value = otp.Trim() };
            var hashParam = new SqlParameter("@NewPasswordHash", SqlDbType.NVarChar, 500) { Value = newPasswordHash };
            var successParam = new SqlParameter("@Success", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            var messageParam = new SqlParameter("@Message", SqlDbType.NVarChar, 250) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_VerifyOtpAndResetPassword_Raj @Email, @Otp, @NewPasswordHash, @Success OUTPUT, @Message OUTPUT",
                emailParam, otpParam, hashParam, successParam, messageParam
            );

            bool success = successParam.Value != DBNull.Value && (bool)successParam.Value;
            string message = messageParam.Value != DBNull.Value ? (string)messageParam.Value : string.Empty;

            return (success, message);
        }
    }
}
