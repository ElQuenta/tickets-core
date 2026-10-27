using Microsoft.EntityFrameworkCore;

namespace tickets_core.Infrastructure.Persistence.Seeds;

public static class DatabaseSeed
{
    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var strategyContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // Un reintento necesita un contexto limpio después de un rollback.
            await using var attempt = services.CreateAsyncScope();
            var context = attempt.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var transaction = await context.Database.BeginTransactionAsync();
            // Serializa ejecuciones del seed sobre la misma base de datos.
            await context.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource = N'TicketsCore.Seed',
                    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
                IF @result < 0 THROW 51010, N'No se pudo bloquear el seed.', 1;
                """);
            await IdentitySeed.RunAsync(attempt.ServiceProvider, configuration);
            await StateSeed.RunAsync(context);
            await transaction.CommitAsync();
        });
    }
}
