using Hangfire.Dashboard;

namespace PayrollSaaS.API.Auth;

/// <summary>Restricts the Hangfire dashboard to super_admin role (doc §6, step 11).</summary>
public sealed class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User.Identity?.IsAuthenticated == true
            && http.User.IsInRole("SuperAdmin");
    }
}
