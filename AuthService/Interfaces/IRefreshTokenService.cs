namespace AuthService.Interfaces
{
    public interface IRefreshTokenService
    {
        string GenerateRefreshToken();
        string HashToken(string token);
    }
}
