using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tickets_core.Infrastructure.Models;

namespace tickets_core.Infrastructure.Persistence.Configurations;

public sealed class StateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.ToTable("STATE");
        builder.HasKey(state => state.Id);
        builder.Property(state => state.Id).HasColumnName("ID");
        builder.Property(state => state.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.HasIndex(state => state.Name).IsUnique().HasDatabaseName("UK_STATE_name");
    }
}
