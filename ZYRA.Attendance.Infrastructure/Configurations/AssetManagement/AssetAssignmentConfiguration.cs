using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRA.Attendance.Infrastructure.Configurations.AssetManagement;

public sealed class AssetAssignmentConfiguration : IEntityTypeConfiguration<AssetAssignment>
{
    public void Configure(EntityTypeBuilder<AssetAssignment> builder)
    {
        builder.ToTable("AssetAssignments", "asset");

        builder.HasKey(x => x.AssetAssignmentId);

        builder.Property(x => x.AssignmentRemarks).HasMaxLength(1000);
        builder.Property(x => x.ReturnRemarks).HasMaxLength(1000);
        builder.Property(x => x.AssignedBy).HasMaxLength(100);
        builder.Property(x => x.ReturnedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.AssetId, x.ReturnedAt })
            .HasDatabaseName("IX_AssetAssignments_AssetId_ReturnedAt");

        // SQL Server allows only one active (not yet returned) assignment per asset.
        builder.HasIndex(x => x.AssetId)
            .IsUnique()
            .HasFilter("[ReturnedAt] IS NULL")
            .HasDatabaseName("UX_AssetAssignments_OneActiveAssignmentPerAsset");

        builder.HasIndex(x => x.EmployeeMappingId)
            .HasDatabaseName("IX_AssetAssignments_EmployeeMappingId");

        builder.HasOne(x => x.EmployeeMapping)
            .WithMany()
            .HasForeignKey(x => x.EmployeeMappingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
