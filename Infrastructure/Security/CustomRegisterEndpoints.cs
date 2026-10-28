using tickets_core.src.Contracts;
using tickets_core.src.Services;

namespace tickets_core.Infrastructure.Security;

public static class CustomRegisterEndpoints
{
    public static IEndpointRouteBuilder MapCustomRegister(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", RegisterAsync)
            .WithTags("Authentication")
            .AllowAnonymous()
            .Produces<LoginResponse>()
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        AuthService authService,
        LinkGenerator links,
        HttpContext context,
        bool? useCookies,
        bool? useSessionCookies)
    {
        var confirmationUrl = links.GetUriByName(context,
            "MapIdentityApi-/api/auth/confirmEmail", values: null)
            ?? throw new InvalidOperationException("No se encontró el endpoint de confirmación de correo.");
        var result = await authService.RegisterAsync(
            request, confirmationUrl, useCookies == true, useSessionCookies == true);

        if (result.Response is null)
            return Results.ValidationProblem(result.Errors!);

        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        return Results.Ok(result.Response);
    }
}
