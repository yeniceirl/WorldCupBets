using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class LookupItemConfiguration : IEntityTypeConfiguration<LookupItem>
{
    public void Configure(EntityTypeBuilder<LookupItem> builder)
    {
        builder.ToTable("lookup_items");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Category).HasMaxLength(50).IsRequired();
        builder.Property(item => item.Key).HasMaxLength(50).IsRequired();
        builder.Property(item => item.Value).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Detail);
        builder.Property(item => item.SortOrder).HasDefaultValue(0);
        builder.Property(item => item.IsActive).HasDefaultValue(true);
        builder.HasIndex(item => new { item.Category, item.Key }).IsUnique();
    }
}
