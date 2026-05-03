using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class LookupOptionConfiguration : IEntityTypeConfiguration<LookupOption>
{
    public void Configure(EntityTypeBuilder<LookupOption> builder)
    {
        builder.ToTable("LookupOptions");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Value).IsRequired().HasMaxLength(100);
        builder.HasIndex(o => new { o.Category, o.IsActive });
    }
}
