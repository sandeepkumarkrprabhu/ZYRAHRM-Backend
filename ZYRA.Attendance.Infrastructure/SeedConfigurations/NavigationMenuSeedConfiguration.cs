using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models;

namespace ZYRA.Attendance.Infrastructure.SeedConfigurations
{
    /// <summary>
    /// Centralized seed configuration for NavigationMenus.
    /// </summary>
    public sealed class NavigationMenuSeedConfiguration : IEntityTypeConfiguration<NavigationMenus>
    {
        public void Configure(EntityTypeBuilder<NavigationMenus> builder)
        {
            builder.HasData(
                                new NavigationMenus
                                {
                                    MenuId = 1,
                                    MenuKey = "dashboard",
                                    ModuleId = 1,
                                    MenuLabel = "Dashboard",
                                    IconName = "LayoutDashboard",
                                    DisplayOrder = 1,
                                    RequiredPermissionCode = "EMP_VIEW",
                                    Description = "Executive overview and quick sync metrics"
                                },
                                new NavigationMenus
                                {
                                    MenuId = 2,
                                    MenuKey = "employees",
                                    ModuleId = 1,
                                    MenuLabel = "Employees",
                                    IconName = "Users",
                                    DisplayOrder = 2,
                                    RequiredPermissionCode = "EMP_VIEW",
                                    Description = "Biometric device ID and employee code mapping"
                                },
                                new NavigationMenus
                                {
                                    MenuId = 3,
                                    MenuKey = "hr-users",
                                    ModuleId = 4,
                                    MenuLabel = "User Access & HR",
                                    IconName = "ShieldCheck",
                                    DisplayOrder = 3,
                                    RequiredPermissionCode = "USER_MANAGE",
                                    Description = "HR administrators, permissions, and menu access matrix"
                                },
                                new NavigationMenus
                                {
                                    MenuId = 4,
                                    MenuKey = "audit-logs",
                                    ModuleId = 4,
                                    MenuLabel = "Audit Logs",
                                    IconName = "FileText",
                                    DisplayOrder = 4,
                                    RequiredPermissionCode = "AUDIT_VIEW",
                                    Description = "Immutable historical activity trail"
                                },
                                new NavigationMenus
                                {
                                    MenuId = 5,
                                    MenuKey = "zyra-api",
                                    ModuleId = 2,
                                    MenuLabel = "Hangfire & ZYRA API",
                                    IconName = "RefreshCw",
                                    DisplayOrder = 5,
                                    RequiredPermissionCode = "SYNC_TRIGGER",
                                    Description = "Attendance synchronization API and Hangfire queue monitor"
                                },
                                new NavigationMenus
                                {
                                    MenuId = 7,
                                    MenuKey = "settings",
                                    ModuleId = 4,
                                    MenuLabel = "Settings",
                                    IconName = "Settings",
                                    DisplayOrder = 7,
                                    RequiredPermissionCode = "SYSTEM_SETTINGS",
                                    Description = "Global system and database parameters"
                                },
                                new NavigationMenus
                                {
                                    MenuId = 13,
                                    MenuKey = "asset-dash",
                                    ModuleId = 5,
                                    MenuLabel = "Dashboard",
                                    IconName = "LayoutDashboard",
                                    DisplayOrder = 1,
                                    RequiredPermissionCode = "ASSET_VIEW",
                                    Description = "Overview of asset inventory, assignments, categories, clients, and vendors"
                                },
                                new NavigationMenus
                                {
                                    MenuId = 9,
                                    MenuKey = "asset-cat",
                                    ModuleId = 5,
                                    MenuLabel = "Asset Categories",
                                    IconName = "Tags",
                                    DisplayOrder = 2,
                                    RequiredPermissionCode = "ASSET_CATEGORY_VIEW",
                                    Description = "Manage categories used to classify company and client assets"
                                },
                                new NavigationMenus
                                {
                                    MenuId = 10,
                                    MenuKey = "assets",
                                    ModuleId = 5,
                                    MenuLabel = "Asset Master",
                                    IconName = "Laptop",
                                    DisplayOrder = 3,
                                    RequiredPermissionCode = "ASSET_VIEW",
                                    Description = "Register and manage owned, rented, and client-owned assets"
                                },
                                new NavigationMenus
                                {
                                    MenuId = 11,
                                    MenuKey = "clients",
                                    ModuleId = 5,
                                    MenuLabel = "Client Master",
                                    IconName = "Building2",
                                    DisplayOrder = 4,
                                    RequiredPermissionCode = "CLIENT_VIEW",
                                    Description = "Manage client records used for client-owned assets"
                                },
                                new NavigationMenus
                                {
                                    MenuId = 12,
                                    MenuKey = "vendors",
                                    ModuleId = 5,
                                    MenuLabel = "Vendor Master",
                                    IconName = "Truck",
                                    DisplayOrder = 5,
                                    RequiredPermissionCode = "VENDOR_VIEW",
                                    Description = "Manage vendors supplying rented assets and services"
                                },
                                new NavigationMenus
                                {
                                    MenuId = 8,
                                    MenuKey = "attpolicy",
                                    ModuleId = 2,
                                    MenuLabel = "Attendance Policies",
                                    IconName = "Settings",
                                    DisplayOrder = 3,
                                    RequiredPermissionCode = "SYSTEM_SETTINGS",
                                    Description = "Global system attendance policies"
                                }
                            );
        }
    }
}
