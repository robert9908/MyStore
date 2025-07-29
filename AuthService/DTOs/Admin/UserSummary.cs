namespace AuthService.DTOs.Admin
{
    public class UserSummary
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
