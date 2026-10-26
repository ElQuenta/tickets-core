using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tickets_core.Infrastructure.Models;

namespace tickets_core.Infrastructure.Persistence.Configurations;

public sealed class HistoryConfiguration : IEntityTypeConfiguration<History>
{
    public void Configure(EntityTypeBuilder<History> builder)
    {
        builder.ToTable("HISTORY");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).HasColumnName("ID");
        builder.Property(history => history.TicketId).HasColumnName("Ticket_id");
        builder.Property(history => history.UserId).HasColumnName("User_id");
        builder.Property(history => history.Comment).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(history => history.PrevState).HasColumnName("prevState").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(history => history.NewState).HasColumnName("newState").HasColumnType("nvarchar(max)").IsRequired();
        builder.HasOne(history => history.Ticket).WithMany(ticket => ticket.Histories)
            .HasForeignKey(history => history.TicketId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(history => history.User).WithMany(user => user.Histories)
            .HasForeignKey(history => history.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
