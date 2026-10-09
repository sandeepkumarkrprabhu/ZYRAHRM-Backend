using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRA.Attendance.Infrastructure.Configurations.AssetManagement;

public sealed class AssetStatusHistoryConfiguration : IEntityTypeConfiguration<AssetStatusHistory>
{
    public void Configure(EntityTypeBuilder<AssetStatusHistory> builder)
    {
        builder.ToTable("AssetStatusHistory", "asset");

        builder.HasKey(x => x.AssetStatusHistoryId);

        builder.Property(x => x.PreviousStatus)
            .HasConversion<int?>();

        builder.Property(x => x.NewStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ChangedBy).HasMaxLength(100);
        builder.Property(x => x.Remarks).HasMaxLength(1000);

        builder.HasIndex(x => new { x.AssetId, x.ChangedAt })
            .HasDatabaseName("IX_AssetStatusHistory_AssetId_ChangedAt")
            .IsDescending(false, true);
    }
}
