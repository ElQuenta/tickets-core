using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tickets_core.Infrastructure.Identity;

namespace tickets_core.Infrastructure.Persistence.Configurations;

public sealed class IdentityConfiguration : IEntityTypeConfiguration<ApplicationUser>,
    IEntityTypeConfiguration<IdentityUserLogin<Guid>>,
    IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // El ER no incluye TwoFactorEnabled. No se habilita 2FA en esta etapa.
        builder.Ignore(user => user.TwoFactorEnabled);
        builder.Property(user => user.Email).HasMaxLength(256);
        builder.Property(user => user.NormalizedEmail).HasMaxLength(256);
        builder.Property(user => user.PhoneNumber).HasMaxLength(256);
        builder.Property(user => user.LockoutEnd).HasColumnType("datetimeoffset");
        builder.HasIndex(user => user.Email).IsUnique().HasDatabaseName("UK_AspNetUsers_Email");
        builder.HasIndex(user => user.NormalizedEmail).IsUnique().HasDatabaseName("EmailIndex");
        builder.HasIndex(user => user.PhoneNumber).IsUnique().HasDatabaseName("UK_AspNetUsers_PhoneNumber");
        // base.OnModelCreating conserva UserNameIndex y RoleNameIndex únicos,
        // las siete tablas de Identity y todas sus PK/FK simples y compuestas.
    }

    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
    {
        // Fijar también en el modelo: no depender de que el contexto se resuelva
        // mediante el contenedor de Identity (herramientas, procesos externos, etc.).
        builder.Property(login => login.LoginProvider).HasMaxLength(128);
        builder.Property(login => login.ProviderKey).HasMaxLength(128);
    }

    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
    {
        builder.Property(token => token.LoginProvider).HasMaxLength(128);
        builder.Property(token => token.Name).HasMaxLength(128);
    }
}
