namespace AuthService.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task BlacklistTokenAsync(string token, string type, TimeSpan expiry);
        Task<bool> IsTokenBlacklistedAsync(string token, string type);
    }
}
