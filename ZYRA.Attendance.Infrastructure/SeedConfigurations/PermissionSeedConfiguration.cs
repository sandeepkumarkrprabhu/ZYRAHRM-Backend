using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models;

namespace ZYRA.Attendance.Infrastructure.SeedConfigurations
{
    /// <summary>
    /// Centralized seed configuration for Permissions.
    /// </summary>
    public sealed class PermissionSeedConfiguration : IEntityTypeConfiguration<Permissions>
    {
        public void Configure(EntityTypeBuilder<Permissions> builder)
        {
            builder.HasData(
                            new Permissions
                            {
                                Id = 1,
                                PermissionCode = "EMP_VIEW",
                                PermissionName = "View Employees",
                                Category = "Employees",
                                Description = "View employee biometric mapping data"
                            },
                            new Permissions
                            {
                                Id = 2,
                                PermissionCode = "EMP_EDIT",
                                PermissionName = "Edit Employees",
                                Category = "Employees",
                                Description = "Modify employee mapping and biometric exclusions"
                            },
                            new Permissions
                            {
                                Id = 3,
                                PermissionCode = "HR_OVERRIDE",
                                PermissionName = "HR Checkout Override",
                                Category = "Attendance",
                                Description = "Override final employee checkout timestamp"
                            },
                            new Permissions
                            {
                                Id = 4,
                                PermissionCode = "USER_MANAGE",
                                PermissionName = "Manage Users & Credentials",
                                Category = "Security",
                                Description = "Provision HR users, assign roleId, and manage credentials"
                            },
                            new Permissions
                            {
                                Id = 5,
                                PermissionCode = "AUDIT_VIEW",
                                PermissionName = "View Audit Trail",
                                Category = "Compliance",
                                Description = "Inspect system audit logs"
                            },
                            new Permissions
                            {
                                Id = 6,
                                PermissionCode = "SYNC_TRIGGER",
                                PermissionName = "Trigger Hangfire Sync",
                                Category = "Attendance",
                                Description = "Execute the attendance synchronization background job"
                            },
                            new Permissions
                            {
                                Id = 7,
                                PermissionCode = "SYSTEM_SETTINGS",
                                PermissionName = "System Configuration",
                                Category = "Settings",
                                Description = "Configure global system parameters"
                            },
                            new Permissions { Id = 8, PermissionCode = "ASSET_CATEGORY_VIEW", PermissionName = "View Asset Categories", Category = "Asset Management", Description = "View asset category records" },
                            new Permissions { Id = 9, PermissionCode = "ASSET_CATEGORY_MANAGE", PermissionName = "Manage Asset Categories", Category = "Asset Management", Description = "Create and update asset categories" },
                            new Permissions { Id = 10, PermissionCode = "ASSET_VIEW", PermissionName = "View Assets", Category = "Asset Management", Description = "View asset master records" },
                            new Permissions { Id = 11, PermissionCode = "ASSET_MANAGE", PermissionName = "Manage Assets", Category = "Asset Management", Description = "Create and update asset master records" },
                            new Permissions { Id = 12, PermissionCode = "CLIENT_VIEW", PermissionName = "View Clients", Category = "Asset Management", Description = "View client master records" },
                            new Permissions { Id = 13, PermissionCode = "CLIENT_MANAGE", PermissionName = "Manage Clients", Category = "Asset Management", Description = "Create and update client master records" },
                            new Permissions { Id = 14, PermissionCode = "VENDOR_VIEW", PermissionName = "View Vendors", Category = "Asset Management", Description = "View vendor master records" },
                            new Permissions { Id = 15, PermissionCode = "VENDOR_MANAGE", PermissionName = "Manage Vendors", Category = "Asset Management", Description = "Create and update vendor master records" }
                        );
        }
    }
}
