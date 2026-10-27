using Microsoft.AspNetCore.Identity;
using tickets_core.Infrastructure.Identity;
using tickets_core.Infrastructure.Persistence;

namespace tickets_core.Infrastructure.Security;

public static class IdentitySecurityConfiguration
{
    public static IServiceCollection AddIdentitySecurity(this IServiceCollection services)
    {
        services.AddIdentityApiEndpoints<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Stores.MaxLengthForKeys = 128;
        }).AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddOptions<SmtpOptions>().BindConfiguration("Smtp");
        services.AddTransient<IEmailSender<ApplicationUser>, IdentityEmailSender>();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });
        services.AddAuthorization();
        return services;
    }
}
