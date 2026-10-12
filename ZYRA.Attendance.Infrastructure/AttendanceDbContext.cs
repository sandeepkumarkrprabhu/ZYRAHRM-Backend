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
            
            

            // Seed initial data for NavigationMenus, Roles, Permissions, and RolePermissions
            

            

            

            
        }
    }
}
