using Microsoft.EntityFrameworkCore;
using ZyraHangfireModels.Models;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRA.Attendance.Infrastructure
{
    public class AttendanceDbContext : DbContext
    {
        public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options)
        : base(options) { }

        public DbSet<EmployeeMapping> EmployeeMappings { get; set; }

        public DbSet<AttendanceLog> AttendanceLogs { get; set; }

        public DbSet<HolidayMaster> Holidiays { get; set; }

        public DbSet<AttendancePolicyMaster> AttendancePolicyMasters { get; set; }

        public DbSet<AttendancePolicyRule> AttendancePolicyRules { get; set; }

        public DbSet<EmployeeAttendancePolicy> EmployeeAttendancePolicies { get; set; }

        public DbSet<NavigationMenus> NavigationMenus { get; set; }

        public DbSet<Modules> Modules { get; set; }

        public DbSet<Roles> Roles { get; set; }
        public DbSet<Permissions> Permissions { get; set; }

        public DbSet<RolePermissions> RolePermissions { get; set; }

        public DbSet<Users> Users { get; set; }

        public DbSet<AuditLog> AuditLog { get; set; }

        public DbSet<Settings> HRMSettings { get; set; }


        // Asset Management module
        public DbSet<ClientMaster> ClientMasters { get; set; }
        public DbSet<ClientEmployeeAssignment> ClientEmployeeAssignments { get; set; }
        public DbSet<VendorMaster> VendorMasters { get; set; }
        public DbSet<AssetCategory> AssetCategories { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetAssignment> AssetAssignments { get; set; }
        public DbSet<AssetStatusHistory> AssetStatusHistory { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Keep module-specific EF Core mappings in the infrastructure project.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AttendanceDbContext).Assembly);
            
            modelBuilder.Entity<Modules>().HasData(
                new Modules { ModuleId = 1, ModuleCode = "HRM", ModuleName = "Human Resource Management", Description = "Employee and HR administration", IsActive = true },
                new Modules { ModuleId = 2, ModuleCode = "ATTENDANCE", ModuleName = "Attendance Management", Description = "Attendance policies and reporting", IsActive = true },
                new Modules { ModuleId = 4, ModuleCode = "ADMIN", ModuleName = "System Administration", Description = "Audit and global system configuration", IsActive = true },
                new Modules { ModuleId = 5, ModuleCode = "ASSET", ModuleName = "Asset Management", Description = "Asset categories, assets, clients, vendors, and assignments", IsActive = true }
            );

            // Seed initial data for NavigationMenus, Roles, Permissions, and RolePermissions
            modelBuilder.Entity<NavigationMenus>().HasData(
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

            modelBuilder.Entity<Roles>().HasData(
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

            modelBuilder.Entity<Permissions>().HasData(
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

            modelBuilder.Entity<RolePermissions>().HasData(
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
