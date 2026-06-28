using System.Text.Json;
using KioskCenter.Services;

namespace KioskCenter.Middleware
{
    public class LicenseMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly string[] WhitelistPaths =
        {
        "/swagger",
        "/openapi/v1.json",
        "/api/info/hardware-id",
        "/api/info/license-status",
        "/api/info/upload-license",
        "/health"
    };

        public LicenseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, LicenseManager licenseManager)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            if (WhitelistPaths.Any(w => path.StartsWith(w)))
            {
                await _next(context);
                return;
            }

            if (!licenseManager.IsLicensed)
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    licensed = false,
                    message = licenseManager.StatusMessage
                }));
                return;
            }

            await _next(context);
        }
    }


}
