using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using tickets_core.Infrastructure.Identity;
using tickets_core.Infrastructure.Models;

namespace tickets_core.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<History> Histories => Set<History>();
    public DbSet<State> States => Set<State>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetTimestamps()
    {
        ChangeTracker.DetectChanges();
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
                entry.Property("CreatedAt").CurrentValue = now;
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Property("UpdatedAt").CurrentValue = now;
            if (entry.State == EntityState.Modified)
                entry.Property("CreatedAt").IsModified = false;
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("dbo");
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        string[] tables = ["TICKET", "HISTORY", "STATE", "AspNetUsers", "AspNetRoles",
            "AspNetUserRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserTokens", "AspNetRoleClaims"];
        foreach (var entity in builder.Model.GetEntityTypes()
                     .Where(entity => tables.Contains(entity.GetTableName())).ToArray())
        {
            var configuration = builder.Entity(entity.ClrType);
            configuration.Property<DateTime>("CreatedAt").HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");
            configuration.Property<DateTime>("UpdatedAt").HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");
            configuration.ToTable(entity.GetTableName()!, entity.GetSchema(), table => table.UseSqlOutputClause(false));
        }
    }
}
