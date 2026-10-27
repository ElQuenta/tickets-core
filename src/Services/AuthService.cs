using Microsoft.AspNetCore.Identity;
using tickets_core.Infrastructure.Identity;
using tickets_core.Infrastructure.Security;
using tickets_core.src.Contracts;

namespace tickets_core.src.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IdentityTokenService tokenService)
{
    public async Task<LoginResult> LoginAsync(
        LoginRequest request, bool useCookies = false, bool useSessionCookies = false)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return new(null, "Credenciales incorrectas.");

        var result = await signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
            return new(null, "Credenciales incorrectas.");

        if (await userManager.GetTwoFactorEnabledAsync(user))
            return new(null, "Esta cuenta requiere un flujo de segundo factor.");

        var username = await userManager.GetUserNameAsync(user);
        var roles = (await userManager.GetRolesAsync(user)).ToArray();

        if (useCookies || useSessionCookies)
        {
            signInManager.AuthenticationScheme = IdentityConstants.ApplicationScheme;
            await signInManager.SignInAsync(user, isPersistent: useCookies && !useSessionCookies);
            return new(new LoginResponse(username, roles));
        }

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var tokens = tokenService.Create(principal);

        return new(new LoginResponse(username, roles, tokens.TokenType,
            tokens.AccessToken, tokens.ExpiresIn, tokens.RefreshToken));
    }
}

public sealed record LoginResult(LoginResponse? Response, string? Error = null);
