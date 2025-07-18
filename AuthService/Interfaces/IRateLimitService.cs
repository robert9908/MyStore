namespace AuthService.Interfaces
{
    public interface IRateLimitService
    {
        Task<bool> IsLimitedAsync(string key, int limit, TimeSpan duration);
    }
}
