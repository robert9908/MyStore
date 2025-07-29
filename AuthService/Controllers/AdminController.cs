using AuthService.DTOs.Admin;
using AuthService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("Users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost("ban/{userId}")]
        public async Task<IActionResult> BanUser(Guid userId)
        {
            var result = await _adminService.BanUserAsync(userId);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpPost("unban/{userId}")]
        public async Task<IActionResult> UnbanUser(Guid userId)
        {
            var result = await _adminService.UnbanUserAsync(userId);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpPost("role")]
        public async Task<IActionResult> ChangeRole([FromBody] ChangeUserRole changeUserRole )
        {
            var result = await _adminService.ChangeUserRoleAsync(changeUserRole.UserId, changeUserRole.NewRole);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var result = await _adminService.DeleteUserAsync(userId);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpPost("confirm-email/{userId}")]
        public async Task<IActionResult> ConfirmEmail(Guid userId)
        {
            var result = await _adminService.ConfirmUserEmailAsync(userId);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}
