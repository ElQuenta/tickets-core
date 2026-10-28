using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Primitives;
using tickets_core.Infrastructure.Identity;

namespace tickets_core.Infrastructure.Security;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var identity = new IdentityRouteBuilder(endpoints);
        identity.MapGroup("/api/auth").WithTags("Authentication").MapIdentityApi<ApplicationUser>();
        foreach (var source in identity.DataSources)
            endpoints.DataSources.Add(new CustomEndpointDataSource(source));
        return endpoints;
    }

    private sealed class IdentityRouteBuilder(IEndpointRouteBuilder parent) : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider => parent.ServiceProvider;
        public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();
        public IApplicationBuilder CreateApplicationBuilder() => parent.CreateApplicationBuilder();
    }

    private sealed class CustomEndpointDataSource(EndpointDataSource source) : EndpointDataSource
    {
        public override IReadOnlyList<Endpoint> Endpoints => source.Endpoints
            .Where(endpoint => endpoint is not RouteEndpoint route ||
                (!string.Equals(route.RoutePattern.RawText?.TrimEnd('/'),
                    "/api/auth/manage/2fa", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(route.RoutePattern.RawText?.TrimEnd('/'),
                    "/api/auth/login", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(route.RoutePattern.RawText?.TrimEnd('/'),
                    "/api/auth/register", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        public override IChangeToken GetChangeToken() => source.GetChangeToken();
    }
}
