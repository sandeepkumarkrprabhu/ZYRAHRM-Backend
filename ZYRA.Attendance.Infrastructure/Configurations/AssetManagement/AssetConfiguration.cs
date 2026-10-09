using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRA.Attendance.Infrastructure.Configurations.AssetManagement;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets", "asset");

        builder.HasKey(x => x.AssetId);

        builder.Property(x => x.AssetCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.AssetCode)
            .IsUnique();

        builder.Property(x => x.AssetName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Manufacturer).HasMaxLength(100);
        builder.Property(x => x.ModelNumber).HasMaxLength(100);
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.DeviceAdminAccountName).HasMaxLength(256);
        builder.Property(x => x.DeviceAdminCredentialSecretReference).HasMaxLength(500);
        builder.Property(x => x.DeviceAdminAccountNotes).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.Property(x => x.OwnershipType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.PurchaseCost)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(x => x.SerialNumber);

        builder.HasMany(x => x.Assignments)
            .WithOne(x => x.Asset)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.StatusHistory)
            .WithOne(x => x.Asset)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
