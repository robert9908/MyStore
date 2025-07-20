namespace AuthService.Interfaces
{
    public interface IRateLimitService
    {
        Task<bool> IsLimitedAsync(string key);
        Task RegisterAttemptAsync(string key);
        Task ResetAttemptsAsync(string key);

    }
}
