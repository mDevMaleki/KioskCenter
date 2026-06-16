using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KioskCenter.Authorization
{
    // فقط کاربرانی که لاگین کرده‌اند و دسترسی به بخش مشخص‌شده را دارند (یا سوپر ادمین هستند) اجازه ورود دارند
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _permission;

        public RequirePermissionAttribute(string permission)
        {
            _permission = permission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "ابتدا وارد شوید" });
                return;
            }

            var isSuperAdmin = user.HasClaim("isSuperAdmin", "true");
            if (isSuperAdmin)
                return;

            if (user.HasClaim("perm", _permission))
                return;

            context.Result = new ForbidObjectResult(new { success = false, message = "شما دسترسی لازم برای این بخش را ندارید" });
        }
    }

    // Forbid با بدنه JSON برای نمایش پیغام مناسب در فرانت
    public class ForbidObjectResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public ForbidObjectResult(object value) : base(value)
        {
            StatusCode = 403;
        }
    }

    public class UnauthorizedObjectResult : Microsoft.AspNetCore.Mvc.ObjectResult
    {
        public UnauthorizedObjectResult(object value) : base(value)
        {
            StatusCode = 401;
        }
    }
}
