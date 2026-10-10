using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRA.Attendance.Infrastructure.Configurations.AssetManagement;

public sealed class VendorMasterConfiguration : IEntityTypeConfiguration<VendorMaster>
{
    public void Configure(EntityTypeBuilder<VendorMaster> builder)
    {
        builder.ToTable("VendorMaster", "asset");
        builder.HasKey(x => x.VendorId);

        builder.Property(x => x.VendorCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.VendorName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ContactPerson).HasMaxLength(150);
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.EmailAddress).HasMaxLength(254);
        builder.Property(x => x.Address).HasMaxLength(1000);
        builder.Property(x => x.TaxRegistrationNumber).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.VendorCode).IsUnique();
        builder.HasIndex(x => x.VendorName);

        builder.HasMany(x => x.Assets)
            .WithOne(x => x.Vendor)
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}