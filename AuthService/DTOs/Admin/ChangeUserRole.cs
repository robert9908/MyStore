namespace AuthService.DTOs.Admin
{
    public class ChangeUserRole
    {
        public Guid UserId { get; set; }
        public string NewRole { get; set; }
    }
}
