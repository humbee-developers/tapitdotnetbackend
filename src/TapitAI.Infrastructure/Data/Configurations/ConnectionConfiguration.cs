using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> builder)
    {
        builder.ToTable("Connections");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.SenderUserId).IsRequired().HasMaxLength(450);
        builder.Property(c => c.ReceiverUserId).IsRequired().HasMaxLength(450);
        builder.Property(c => c.SenderInvitationMessage).HasMaxLength(500);
        builder.Property(c => c.ReceiverInvitationMessage).HasMaxLength(500);
        builder.Property(c => c.SenderConnectionMessage).HasMaxLength(1000);
        builder.Property(c => c.ReceiverConnectionMessage).HasMaxLength(1000);
        builder.Property(c => c.ChatChannelId).HasMaxLength(200);

        builder.HasIndex(c => new { c.SenderUserId, c.InvitationStatus });
        builder.HasIndex(c => new { c.ReceiverUserId, c.InvitationStatus });
        builder.HasIndex(c => c.InvitedAt);
    }
}
