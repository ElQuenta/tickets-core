using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace tickets_core.Infrastructure.Security;

public sealed class IdentityTokenService(
    IOptionsMonitor<BearerTokenOptions> bearerOptions,
    TimeProvider timeProvider)
{
    public AccessTokenResponse Create(ClaimsPrincipal principal)
    {
        var scheme = IdentityConstants.BearerScheme;
        var options = bearerOptions.Get(scheme);
        var now = timeProvider.GetUtcNow();

        var accessTicket = new AuthenticationTicket(principal,
            new AuthenticationProperties { ExpiresUtc = now + options.BearerTokenExpiration },
            $"{scheme}:AccessToken");

        var refreshTicket = new AuthenticationTicket(principal,
            new AuthenticationProperties { ExpiresUtc = now + options.RefreshTokenExpiration },
            $"{scheme}:RefreshToken");

        return new AccessTokenResponse
        {
            AccessToken = options.BearerTokenProtector.Protect(accessTicket),
            ExpiresIn = (long)options.BearerTokenExpiration.TotalSeconds,
            RefreshToken = options.RefreshTokenProtector.Protect(refreshTicket)
        };
    }
}
