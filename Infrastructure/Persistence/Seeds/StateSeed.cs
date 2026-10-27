using Microsoft.EntityFrameworkCore;
using tickets_core.Infrastructure.Models;

namespace tickets_core.Infrastructure.Persistence.Seeds;

public static class StateSeed
{
    public static readonly string[] Names = ["Pendiente", "En Proceso", "Resuelto"];

    public static async Task RunAsync(ApplicationDbContext context)
    {
        foreach (var name in Names)
            if (!await context.States.AnyAsync(state => state.Name == name))
                context.States.Add(new State { Name = name });
        await context.SaveChangesAsync();
    }
}
