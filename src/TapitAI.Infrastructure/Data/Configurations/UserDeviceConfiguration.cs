using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.ToTable("UserDevices");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.UserId).IsRequired().HasMaxLength(450);
        builder.Property(d => d.FcmToken).IsRequired().HasMaxLength(500);
        builder.Property(d => d.DevicePlatform).HasMaxLength(20);
        builder.HasIndex(d => new { d.UserId, d.DevicePlatform });
    }
}
