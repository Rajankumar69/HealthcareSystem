using AuthService.Models;

namespace AuthService.Services
{
    public interface ITokenService
    {
        string GenerateToken(User_Raj user, string roleName);
    }
}
