using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models;

namespace ZYRA.Attendance.Infrastructure.SeedConfigurations
{
    /// <summary>
    /// Centralized seed configuration for Modules.
    /// </summary>
    public sealed class ModuleSeedConfiguration : IEntityTypeConfiguration<Modules>
    {
        public void Configure(EntityTypeBuilder<Modules> builder)
        {
            builder.HasData(
                            new Modules { ModuleId = 1, ModuleCode = "HRM", ModuleName = "Human Resource Management", Description = "Employee and HR administration", IsActive = true },
                            new Modules { ModuleId = 2, ModuleCode = "ATTENDANCE", ModuleName = "Attendance Management", Description = "Attendance policies and reporting", IsActive = true },
                            new Modules { ModuleId = 4, ModuleCode = "ADMIN", ModuleName = "System Administration", Description = "Audit and global system configuration", IsActive = true },
                            new Modules { ModuleId = 5, ModuleCode = "ASSET", ModuleName = "Asset Management", Description = "Asset categories, assets, clients, vendors, and assignments", IsActive = true }
                        );
        }
    }
}
