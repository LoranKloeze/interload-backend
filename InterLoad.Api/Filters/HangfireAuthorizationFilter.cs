using Hangfire.Dashboard;

namespace InterLoad.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("hangfire");
    }
   
}