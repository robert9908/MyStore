using AuthService.DTOs.Admin;

namespace AuthService.Interfaces
{
    public interface IAdminService
    {
        Task<List<UserSummary>> GetAllUsersAsync();
        Task<bool> BanUserAsync(Guid userId);
        Task<bool> UnbanUserAsync(Guid userId);
        Task<bool> ChangeUserRoleAsync(Guid userId, string newRole);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<bool> ConfirmUserEmailAsync(Guid userId);
    }
}
