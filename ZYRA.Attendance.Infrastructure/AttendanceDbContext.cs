using Microsoft.EntityFrameworkCore;
using ZyraHangfireModels.Models;

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

        public DbSet<Roles> Roles { get; set; }
        public DbSet<Permissions> Permissions { get; set; }

        public DbSet<RolePermissions> RolePermissions { get; set; }

        public DbSet<Users> Users { get; set; }

        public DbSet<AuditLog> AuditLog { get; set; }

        public DbSet<Settings> HRMSettings { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Seed initial data for NavigationMenus, Roles, Permissions, and RolePermissions
            modelBuilder.Entity<NavigationMenus>().HasData(
                    new NavigationMenus
                    {
                        MenuId = 1,
                        MenuKey = "dashboard",
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
                        MenuLabel = "Hangfire & ZYRA API",
                        IconName = "RefreshCw",
                        DisplayOrder = 5,
                        RequiredPermissionCode = "SYNC_TRIGGER",
                        Description = "Single API contract and Hangfire queue monitor"
                    },
                    new NavigationMenus
                    {
                        MenuId = 7,
                        MenuKey = "settings",
                        MenuLabel = "Settings",
                        IconName = "Settings",
                        DisplayOrder = 7,
                        RequiredPermissionCode = "SYSTEM_SETTINGS",
                        Description = "Global system and database parameters"
                    },
                    new NavigationMenus
                    {
                        MenuId = 8,
                        MenuKey = "attpolicy",
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
                    Category = "Integration",
                    Description = "Execute ProcessAttendanceSyncJob background job"
                },
                new Permissions
                {
                    Id = 7,
                    PermissionCode = "SYSTEM_SETTINGS",
                    PermissionName = "System Configuration",
                    Category = "Settings",
                    Description = "Configure global system parameters"
                }
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
                new RolePermissions { Id = 13, RoleId = 4, PermissionId = 5 }
            );
        }
    }
}
