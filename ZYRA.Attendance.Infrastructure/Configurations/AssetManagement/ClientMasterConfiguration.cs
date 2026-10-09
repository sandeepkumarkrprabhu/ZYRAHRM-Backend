using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRA.Attendance.Infrastructure.Configurations.AssetManagement;

public sealed class ClientMasterConfiguration : IEntityTypeConfiguration<ClientMaster>
{
    public void Configure(EntityTypeBuilder<ClientMaster> builder)
    {
        builder.ToTable("ClientMaster", "asset");
        builder.HasKey(x => x.ClientId);

        builder.Property(x => x.ClientCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ClientName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.ClientCode).IsUnique();
        builder.HasIndex(x => x.ClientName).IsUnique();

        builder.HasMany(x => x.EmployeeAssignments)
            .WithOne(x => x.Client)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Assets)
            .WithOne(x => x.Client)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
