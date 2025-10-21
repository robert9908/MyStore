using AuthService.Interfaces;

namespace AuthService.Middlewares
{
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ITokenBlacklistService _blacklistService;

        public TokenBlacklistMiddleware(RequestDelegate next, ITokenBlacklistService blacklistService)
        {
            _next = next;
            _blacklistService = blacklistService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer", "");

            if(!string.IsNullOrWhiteSpace(token) && await _blacklistService.IsTokenBlacklistedAsync(token, "access"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Token is blacklisted");
                return;
            }
            await _next(context);
        }
    }
}
