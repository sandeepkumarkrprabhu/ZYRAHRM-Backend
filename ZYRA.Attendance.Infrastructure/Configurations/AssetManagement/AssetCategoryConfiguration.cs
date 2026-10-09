using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRA.Attendance.Infrastructure.Configurations.AssetManagement;

public sealed class AssetCategoryConfiguration : IEntityTypeConfiguration<AssetCategory>
{
    public void Configure(EntityTypeBuilder<AssetCategory> builder)
    {
        builder.ToTable("AssetCategories", "asset");

        builder.HasKey(x => x.AssetCategoryId);

        builder.Property(x => x.CategoryName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.CategoryName)
            .IsUnique();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.HasMany(x => x.Assets)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.AssetCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
