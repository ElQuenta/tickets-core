using Microsoft.EntityFrameworkCore;
using tickets_core.Infrastructure.Persistence;
using tickets_core.Infrastructure.Security;
using tickets_core.src.Repositories;
using tickets_core.src.Services;

namespace tickets_core.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Configura ConnectionStrings__DefaultConnection en el entorno o en el archivo .env de la raíz del proyecto.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        services.AddIdentitySecurity();
        services.AddScoped<HistoryRepository>();
        services.AddScoped<TicketRepository>();
        services.AddScoped<TicketService>();
        services.AddScoped<StateRepository>();
        services.AddScoped<StateService>();
        return services;
    }
}
