
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

        public async Task<bool> IsTokenBlacklistedAsync(string token, string type)
        {
            return await _db.KeyExistsAsync(GetKey(token, type));
        }

        private static string GetKey(string token, string type) => $"Blacklist{type}:{token}";

        public async Task BlacklistTokenAsync(string token, string type, TimeSpan expiry)
        {
            await _db.StringSetAsync(GetKey(token, type), "1", expiry);
        }
    }
}
