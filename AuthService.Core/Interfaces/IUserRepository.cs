using AuthService.Models;

namespace AuthService.Repositories
{
    public interface IUserRepository
    {
        Task<User_Raj?> GetByEmailAsync(string email);
        Task<User_Raj?> GetByIdAsync(int userId);
        Task<List<User_Raj>> GetUsersByRoleAsync(string roleName);
        Task<Role_Raj?> GetRoleByNameAsync(string roleName);
        Task<User_Raj> AddUserAsync(User_Raj user);
        Task<bool> UpdatePasswordAsync(int userId, string newPasswordHash);
        Task<List<Department_Raj>> GetDepartmentsAsync();
    }
}
