namespace AuthService.Interfaces
{
    public interface IJwtService
    {
        DateTime GetExpiryFromToken(string token);
    }
}
