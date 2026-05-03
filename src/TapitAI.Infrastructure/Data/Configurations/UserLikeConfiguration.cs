using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class UserLikeConfiguration : IEntityTypeConfiguration<UserLike>
{
    public void Configure(EntityTypeBuilder<UserLike> builder)
    {
        builder.ToTable("UserLikes");
        builder.HasKey(ul => ul.Id);
        builder.Property(ul => ul.LikerId).IsRequired().HasMaxLength(450);
        builder.Property(ul => ul.LikedUserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(ul => new { ul.LikerId, ul.LikedUserId }).IsUnique();
    }
}
