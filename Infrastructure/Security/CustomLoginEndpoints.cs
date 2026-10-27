using tickets_core.src.Contracts;
using tickets_core.src.Services;

namespace tickets_core.Infrastructure.Security;

public static class CustomLoginEndpoints
{
    public static IEndpointRouteBuilder MapCustomLogin(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", LoginAsync)
            .WithTags("Authentication")
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AuthService authService,
        HttpContext context,
        bool? useCookies,
        bool? useSessionCookies)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new LoginErrorResponse("El correo electrónico y la contraseña son obligatorios."));

        var result = await authService.LoginAsync(request, useCookies == true, useSessionCookies == true);

        if (result.Response is null)
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: result.Error);

        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";

        return Results.Ok(result.Response);
    }
}
