
using AuthService.Interfaces;
using StackExchange.Redis;

namespace AuthService.Services
{
    public class RedisTokenBlacklistService: ITokenBlacklistService
    {
        private readonly IDatabase _db;
        public RedisTokenBlacklistService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            return await _db.KeyExistsAsync(GetKey(token));
        }

        private static string GetKey(string token) => $"Blacklist:{token}";

        public async Task BlacklistTokenAsync(string token, TimeSpan expiry)
        {
            await _db.StringSetAsync(GetKey(token), "1", expiry);
        }
    }
}
