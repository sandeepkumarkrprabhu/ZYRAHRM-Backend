using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models;

namespace ZYRA.Attendance.Infrastructure.SeedConfigurations
{
    /// <summary>
    /// Centralized seed configuration for Roles.
    /// </summary>
    public sealed class RoleSeedConfiguration : IEntityTypeConfiguration<Roles>
    {
        public void Configure(EntityTypeBuilder<Roles> builder)
        {
            builder.HasData(
                            new Roles
                            {
                                Id = 1,
                                RoleName = "Super Admin",
                                Description = "Full unrestricted system administration, user provisioning, and DB schema access"
                            },
                            new Roles
                            {
                                Id = 2,
                                RoleName = "HR Admin",
                                Description = "Employee management, HR overrides, and attendance log synchronization"
                            },
                            new Roles
                            {
                                Id = 3,
                                RoleName = "Attendance Manager",
                                Description = "Employee biometric verification and attendance reports monitoring"
                            },
                            new Roles
                            {
                                Id = 4,
                                RoleName = "Auditor",
                                Description = "Read-only audit trail and SQL Server schema compliance inspector"
                            },
                            new Roles
                            {
                                Id = 5,
                                RoleName = "IT Admin",
                                Description = "Manage IT assets, asset categories, clients, vendors, and asset lifecycle"
                            },
                            new Roles
                            {
                                Id = 6,
                                RoleName = "Asset Manager",
                                Description = "Maintain asset records, categories, client records, and vendor records"
                            }
                        );
        }
    }
}
