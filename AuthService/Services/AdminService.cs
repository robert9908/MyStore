using AuthService.DTOs.Admin;
using AuthService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services
{
    public class AdminService: IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserSummary>> GetAllUsersAsync()
        {
            return await _context.Users
                .Select(u => new UserSummary
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = u.Role,
                    IsBanned = u.IsBanned,
                    EmailConfirmed = u.EmailConfirmed

                })
                .ToListAsync();
        }

        public async Task<bool> BanUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if(user == null) return false;

            user.IsBanned = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeUserRoleAsync(Guid userId, string newRole)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.Role = newRole;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConfirmUserEmailAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.EmailConfirmed = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> UnbanUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsBanned = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
