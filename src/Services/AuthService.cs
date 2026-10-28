using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using tickets_core.Infrastructure.Identity;
using tickets_core.Infrastructure.Security;
using tickets_core.src.Contracts;

namespace tickets_core.src.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IdentityTokenService tokenService,
    IEmailSender<ApplicationUser> emailSender)
{
    public async Task<RegisterResult> RegisterAsync(
        RegisterRequest request, string confirmationEndpointUrl,
        bool useCookies = false, bool useSessionCookies = false)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Username))
            errors["username"] = ["El nombre de usuario es obligatorio."];
        if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email))
            errors["email"] = ["El correo electrónico debe ser válido."];
        if (string.IsNullOrWhiteSpace(request.Password))
            errors["password"] = ["La contraseña es obligatoria."];
        if (errors.Count > 0)
            return new(null, errors);

        var user = new ApplicationUser { UserName = request.Username, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return new(null, result.Errors.GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));

        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = QueryHelpers.AddQueryString(confirmationEndpointUrl,
            new Dictionary<string, string?>
            {
                ["userId"] = user.Id.ToString(),
                ["code"] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code))
            });
        await emailSender.SendConfirmationLinkAsync(user, request.Email, HtmlEncoder.Default.Encode(confirmationLink));

        return new(await CreateSessionAsync(user, useCookies, useSessionCookies));
    }

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

        return new(await CreateSessionAsync(user, useCookies, useSessionCookies));
    }

    private async Task<LoginResponse> CreateSessionAsync(
        ApplicationUser user, bool useCookies, bool useSessionCookies)
    {
        var username = await userManager.GetUserNameAsync(user);
        var roles = (await userManager.GetRolesAsync(user)).ToArray();

        if (useCookies || useSessionCookies)
        {
            signInManager.AuthenticationScheme = IdentityConstants.ApplicationScheme;
            await signInManager.SignInAsync(user, isPersistent: useCookies && !useSessionCookies);
            return new LoginResponse(username, roles);
        }

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var tokens = tokenService.Create(principal);

        return new LoginResponse(username, roles, tokens.TokenType,
            tokens.AccessToken, tokens.ExpiresIn, tokens.RefreshToken);
    }
}

public sealed record LoginResult(LoginResponse? Response, string? Error = null);

public sealed record RegisterResult(LoginResponse? Response, Dictionary<string, string[]>? Errors = null);
