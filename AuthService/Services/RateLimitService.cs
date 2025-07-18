using AuthService.Interfaces;
using StackExchange.Redis;

namespace AuthService.Services
{
    public class RateLimitService : IRateLimitService
    {
        private readonly IDatabase _redis;
        public RateLimitService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }
        public async Task<bool> IsLimitedAsync(string key, int limit, TimeSpan duration)
        {
            var count = await _redis.StringIncrementAsync(key);

            if(count == 1)
            {
                await _redis.KeyExpireAsync(key, duration);
            }

            return count > limit;
        }
    }
}
