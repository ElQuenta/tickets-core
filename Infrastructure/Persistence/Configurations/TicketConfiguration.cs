using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tickets_core.Infrastructure.Models;

namespace tickets_core.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("TICKET");
        builder.Property(ticket => ticket.DeletedAt).HasColumnType("datetime2");
        builder.HasKey(ticket => ticket.Id);
        builder.Property(ticket => ticket.Id).HasColumnName("ID");
        builder.Property(ticket => ticket.UserId).HasColumnName("User_id");
        builder.Property(ticket => ticket.StateId).HasColumnName("State_id");
        builder.Property(ticket => ticket.Title).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(ticket => ticket.Description).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasOne(ticket => ticket.User).WithMany(user => user.Tickets)
            .HasForeignKey(ticket => ticket.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(ticket => ticket.State).WithMany(state => state.Tickets)
            .HasForeignKey(ticket => ticket.StateId).OnDelete(DeleteBehavior.NoAction);
    }
}
