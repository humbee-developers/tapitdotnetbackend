using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class TapStatusConfiguration : IEntityTypeConfiguration<TapStatus>
{
    public void Configure(EntityTypeBuilder<TapStatus> builder)
    {
        builder.ToTable("TapStatuses");
        builder.HasKey(ts => ts.Id);
        builder.Property(ts => ts.UserId).IsRequired().HasMaxLength(450);
        builder.Property(ts => ts.TapOutReason).HasMaxLength(500);
        builder.HasIndex(ts => ts.UserId).IsUnique();
    }
}
