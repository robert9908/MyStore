using AuthService.Interfaces;

namespace AuthService.Middlewares
{
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenBlacklistMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
        {
            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer", "");

            if(!string.IsNullOrWhiteSpace(token) && await blacklistService.IsTokenBlacklistedAsync(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Token is blacklisted");
                return;
            }
            await _next(context);
        }
    }
}
