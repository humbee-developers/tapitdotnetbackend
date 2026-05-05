using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.ToTable("UserBlocks");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BlockerUserId).IsRequired().HasMaxLength(450);
        builder.Property(b => b.BlockedUserId).IsRequired().HasMaxLength(450);

        builder.HasIndex(b => new { b.BlockerUserId, b.BlockedUserId }).IsUnique();
    }
}
