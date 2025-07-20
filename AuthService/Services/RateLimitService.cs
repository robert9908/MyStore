using AuthService.Interfaces;
using StackExchange.Redis;

namespace AuthService.Services
{
    public class RateLimitService : IRateLimitService
    {
        private readonly IDatabase _redis;
        private const int MaxAttempts = 3;
        private readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(10);
        public RateLimitService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }
        public async Task<bool> IsLimitedAsync(string key)
        {
            var attempts = await _redis.StringGetAsync(key);
            if(attempts.IsNullOrEmpty) return false;

            return int.Parse(attempts!) >= MaxAttempts;
        }

        public async Task RegisterAttemptAsync(string key)
        {
            var exists = await _redis.KeyExistsAsync(key);
            if(exists)
            {
                await _redis.StringIncrementAsync(key);
            }
            else
            {
                await _redis.StringSetAsync(key, 1, _lockoutDuration);
            }
        }

        public async Task ResetAttemptsAsync(string key)
        {
            await _redis.KeyDeleteAsync(key);
        }
    }
}
