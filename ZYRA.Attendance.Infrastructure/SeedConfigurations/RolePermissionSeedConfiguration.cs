using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models;

namespace ZYRA.Attendance.Infrastructure.SeedConfigurations
{
    /// <summary>
    /// Centralized seed configuration for RolePermissions.
    /// </summary>
    public sealed class RolePermissionSeedConfiguration : IEntityTypeConfiguration<RolePermissions>
    {
        public void Configure(EntityTypeBuilder<RolePermissions> builder)
        {
            builder.HasData(
                            new RolePermissions { Id = 1, RoleId = 1, PermissionId = 1 },
                            new RolePermissions { Id = 2, RoleId = 1, PermissionId = 2 },
                            new RolePermissions { Id = 3, RoleId = 1, PermissionId = 3 },
                            new RolePermissions { Id = 4, RoleId = 1, PermissionId = 4 },
                            new RolePermissions { Id = 5, RoleId = 1, PermissionId = 5 },
                            new RolePermissions { Id = 6, RoleId = 1, PermissionId = 6 },
                            new RolePermissions { Id = 7, RoleId = 1, PermissionId = 7 },
                            new RolePermissions { Id = 8, RoleId = 2, PermissionId = 1 },
                            new RolePermissions { Id = 9, RoleId = 2, PermissionId = 2 },
                            new RolePermissions { Id = 10, RoleId = 2, PermissionId = 3 },
                            new RolePermissions { Id = 11, RoleId = 3, PermissionId = 1 },
                            new RolePermissions { Id = 12, RoleId = 3, PermissionId = 5 },
                            new RolePermissions { Id = 13, RoleId = 4, PermissionId = 5 },
                            // Super Admin: all Asset Management permissions.
                            new RolePermissions { Id = 14, RoleId = 1, PermissionId = 8 },
                            new RolePermissions { Id = 15, RoleId = 1, PermissionId = 9 },
                            new RolePermissions { Id = 16, RoleId = 1, PermissionId = 10 },
                            new RolePermissions { Id = 17, RoleId = 1, PermissionId = 11 },
                            new RolePermissions { Id = 18, RoleId = 1, PermissionId = 12 },
                            new RolePermissions { Id = 19, RoleId = 1, PermissionId = 13 },
                            new RolePermissions { Id = 20, RoleId = 1, PermissionId = 14 },
                            new RolePermissions { Id = 21, RoleId = 1, PermissionId = 15 },
                            // HR Admin: manage asset records and supporting masters.
                            new RolePermissions { Id = 22, RoleId = 2, PermissionId = 8 },
                            new RolePermissions { Id = 23, RoleId = 2, PermissionId = 9 },
                            new RolePermissions { Id = 24, RoleId = 2, PermissionId = 10 },
                            new RolePermissions { Id = 25, RoleId = 2, PermissionId = 11 },
                            new RolePermissions { Id = 26, RoleId = 2, PermissionId = 12 },
                            new RolePermissions { Id = 27, RoleId = 2, PermissionId = 13 },
                            new RolePermissions { Id = 28, RoleId = 2, PermissionId = 14 },
                            new RolePermissions { Id = 29, RoleId = 2, PermissionId = 15 },
                            // IT Admin: full operational access to Asset Management.
                            new RolePermissions { Id = 30, RoleId = 5, PermissionId = 8 },
                            new RolePermissions { Id = 31, RoleId = 5, PermissionId = 9 },
                            new RolePermissions { Id = 32, RoleId = 5, PermissionId = 10 },
                            new RolePermissions { Id = 33, RoleId = 5, PermissionId = 11 },
                            new RolePermissions { Id = 34, RoleId = 5, PermissionId = 12 },
                            new RolePermissions { Id = 35, RoleId = 5, PermissionId = 13 },
                            new RolePermissions { Id = 36, RoleId = 5, PermissionId = 14 },
                            new RolePermissions { Id = 37, RoleId = 5, PermissionId = 15 },
                            // Asset Manager: full maintenance access to Asset Management master data.
                            new RolePermissions { Id = 38, RoleId = 6, PermissionId = 8 },
                            new RolePermissions { Id = 39, RoleId = 6, PermissionId = 9 },
                            new RolePermissions { Id = 40, RoleId = 6, PermissionId = 10 },
                            new RolePermissions { Id = 41, RoleId = 6, PermissionId = 11 },
                            new RolePermissions { Id = 42, RoleId = 6, PermissionId = 12 },
                            new RolePermissions { Id = 43, RoleId = 6, PermissionId = 13 },
                            new RolePermissions { Id = 44, RoleId = 6, PermissionId = 14 },
                            new RolePermissions { Id = 45, RoleId = 6, PermissionId = 15 },
                            // Auditor: read-only access to asset and supporting master data.
                            new RolePermissions { Id = 46, RoleId = 4, PermissionId = 8 },
                            new RolePermissions { Id = 47, RoleId = 4, PermissionId = 10 },
                            new RolePermissions { Id = 48, RoleId = 4, PermissionId = 12 },
                            new RolePermissions { Id = 49, RoleId = 4, PermissionId = 14 }
                        );
        }
    }
}
