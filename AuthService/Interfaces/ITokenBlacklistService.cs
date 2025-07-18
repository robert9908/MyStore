﻿namespace AuthService.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task BlacklistTokenAsync(string token, TimeSpan expiry);
        Task<bool> IsTokenBlacklistedAsync(string token);
    }
}
