using Microsoft.AspNetCore.Identity;

namespace smart_feedback.Middleware
{
    public class ForcePasswordChangeMiddleware
    {
        private readonly RequestDelegate _next;

        public ForcePasswordChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<IdentityUser> userManager)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null)
                {
                    var requiresPasswordChange = await userManager.GetAuthenticationTokenAsync(user, "Default", "RequirePasswordChange");

                    if (requiresPasswordChange == "true" &&
                        !context.Request.Path.StartsWithSegments("/Identity/Account/ForcePasswordChange") &&
                        !context.Request.Path.StartsWithSegments("/Identity/Account/Logout"))
                    {
                        context.Response.Redirect("/Identity/Account/ForcePasswordChange");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
