using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class UserReportConfiguration : IEntityTypeConfiguration<UserReport>
{
    public void Configure(EntityTypeBuilder<UserReport> builder)
    {
        builder.ToTable("UserReports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReporterUserId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.ReportedUserId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.Reason).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(1000);
    }
}
