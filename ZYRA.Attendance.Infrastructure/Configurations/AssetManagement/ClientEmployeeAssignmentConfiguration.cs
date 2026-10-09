using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRA.Attendance.Infrastructure.Configurations.AssetManagement;

public sealed class ClientEmployeeAssignmentConfiguration : IEntityTypeConfiguration<ClientEmployeeAssignment>
{
    public void Configure(EntityTypeBuilder<ClientEmployeeAssignment> builder)
    {
        builder.ToTable("ClientEmployeeAssignments", "asset");
        builder.HasKey(x => x.ClientEmployeeAssignmentId);

        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.AssignedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.ClientId, x.EmployeeMappingId, x.EffectiveTo })
            .HasDatabaseName("IX_ClientEmployeeAssignments_Client_Employee_EffectiveTo");

        builder.HasIndex(x => new { x.EmployeeMappingId, x.EffectiveTo })
            .HasDatabaseName("IX_ClientEmployeeAssignments_Employee_EffectiveTo");

        // Prevent overlapping open-ended active assignments for the same client/employee pair.
        builder.HasIndex(x => new { x.ClientId, x.EmployeeMappingId })
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL")
            .HasDatabaseName("UX_ClientEmployeeAssignments_OneActiveClientPerEmployeePair");

        builder.HasOne(x => x.EmployeeMapping)
            .WithMany()
            .HasForeignKey(x => x.EmployeeMappingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
