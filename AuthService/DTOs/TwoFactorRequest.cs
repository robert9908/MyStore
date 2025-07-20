namespace AuthService.DTOs
{
    public class TwoFactorRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
