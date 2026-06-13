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
        "/health"
    };

        public LicenseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, LicenseValidator validator)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            if (WhitelistPaths.Any(w => path.StartsWith(w)))
            {
                await _next(context);
                return;
            }

            try
            {
                validator.Validate("license.dat");


                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync($"License Error: {ex.Message}");
            }
        }
    }


}
