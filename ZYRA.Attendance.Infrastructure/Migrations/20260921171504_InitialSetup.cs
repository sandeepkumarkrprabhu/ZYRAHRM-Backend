using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZYRA.Attendance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CheckTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttendanceState = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendancePolicyMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PolicyType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsEnable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendancePolicyMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    userEmail = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    userName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Holidiays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HolidayCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HolidayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HolidayType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidiays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HRMSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingsName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingsValue = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRMSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NavigationMenus",
                columns: table => new
                {
                    MenuId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuKey = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MenuLabel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IconName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    RequiredPermissionCode = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationMenus", x => x.MenuId);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PermissionName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    IsPasswordResetRequired = table.Column<bool>(type: "bit", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastLoginDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendancePolicyRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendancePolicyId = table.Column<int>(type: "int", nullable: true),
                    RuleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RuleValue = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendancePolicyRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendancePolicyRules_AttendancePolicyMasters_AttendancePolicyId",
                        column: x => x.AttendancePolicyId,
                        principalTable: "AttendancePolicyMasters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BiometricUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HRMEmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsExcludeFromBiometric = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LatestCheckoutFromBiometric = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastCheckoutFinal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCheckoutFinalOverriddenByHR = table.Column<bool>(type: "bit", nullable: false),
                    AttendancePolicyMasterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeMappings_AttendancePolicyMasters_AttendancePolicyMasterId",
                        column: x => x.AttendancePolicyMasterId,
                        principalTable: "AttendancePolicyMasters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAttendancePolicies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AttendancePolicyId = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AutoCheckInTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AutoCheckOutTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeMappingId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAttendancePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendancePolicies_AttendancePolicyMasters_AttendancePolicyId",
                        column: x => x.AttendancePolicyId,
                        principalTable: "AttendancePolicyMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendancePolicies_EmployeeMappings_EmployeeMappingId",
                        column: x => x.EmployeeMappingId,
                        principalTable: "EmployeeMappings",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "NavigationMenus",
                columns: new[] { "MenuId", "Description", "DisplayOrder", "IconName", "MenuKey", "MenuLabel", "RequiredPermissionCode" },
                values: new object[,]
                {
                    { 1, "Executive overview and quick sync metrics", 1, "LayoutDashboard", "dashboard", "Dashboard", "EMP_VIEW" },
                    { 2, "Biometric device ID and employee code mapping", 2, "Users", "employees", "Employees", "EMP_VIEW" },
                    { 3, "HR administrators, permissions, and menu access matrix", 3, "ShieldCheck", "hr-users", "User Access & HR", "USER_MANAGE" },
                    { 4, "Immutable historical activity trail", 4, "FileText", "audit-logs", "Audit Logs", "AUDIT_VIEW" },
                    { 5, "Single API contract and Hangfire queue monitor", 5, "RefreshCw", "zyra-api", "Hangfire & ZYRA API", "SYNC_TRIGGER" },
                    { 7, "Global system and database parameters", 7, "Settings", "settings", "Settings", "SYSTEM_SETTINGS" },
                    { 8, "Global system attendance policies", 3, "Settings", "attpolicy", "Attendance Policies", "SYSTEM_SETTINGS" }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "PermissionCode", "PermissionName" },
                values: new object[,]
                {
                    { 1L, "Employees", "View employee biometric mapping data", "EMP_VIEW", "View Employees" },
                    { 2L, "Employees", "Modify employee mapping and biometric exclusions", "EMP_EDIT", "Edit Employees" },
                    { 3L, "Attendance", "Override final employee checkout timestamp", "HR_OVERRIDE", "HR Checkout Override" },
                    { 4L, "Security", "Provision HR users, assign roleId, and manage credentials", "USER_MANAGE", "Manage Users & Credentials" },
                    { 5L, "Compliance", "Inspect system audit logs", "AUDIT_VIEW", "View Audit Trail" },
                    { 6L, "Integration", "Execute ProcessAttendanceSyncJob background job", "SYNC_TRIGGER", "Trigger Hangfire Sync" },
                    { 7L, "Settings", "Configure global system parameters", "SYSTEM_SETTINGS", "System Configuration" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 1, 1 },
                    { 2, 2, 1 },
                    { 3, 3, 1 },
                    { 4, 4, 1 },
                    { 5, 5, 1 },
                    { 6, 6, 1 },
                    { 7, 7, 1 },
                    { 8, 1, 2 },
                    { 9, 2, 2 },
                    { 10, 3, 2 },
                    { 11, 1, 3 },
                    { 12, 5, 3 },
                    { 13, 5, 4 }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedDateTime", "Description", "IsActive", "RoleName", "UpdatedDateTime" },
                values: new object[,]
                {
                    { 1L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Full unrestricted system administration, user provisioning, and DB schema access", false, "Super Admin", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Employee management, HR overrides, and attendance log synchronization", false, "HR Admin", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Employee biometric verification and attendance reports monitoring", false, "Attendance Manager", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Read-only audit trail and SQL Server schema compliance inspector", false, "Auditor", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePolicyRules_AttendancePolicyId",
                table: "AttendancePolicyRules",
                column: "AttendancePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendancePolicies_AttendancePolicyId",
                table: "EmployeeAttendancePolicies",
                column: "AttendancePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendancePolicies_EmployeeMappingId",
                table: "EmployeeAttendancePolicies",
                column: "EmployeeMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMappings_AttendancePolicyMasterId",
                table: "EmployeeMappings",
                column: "AttendancePolicyMasterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceLogs");

            migrationBuilder.DropTable(
                name: "AttendancePolicyRules");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "EmployeeAttendancePolicies");

            migrationBuilder.DropTable(
                name: "Holidiays");

            migrationBuilder.DropTable(
                name: "HRMSettings");

            migrationBuilder.DropTable(
                name: "NavigationMenus");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "EmployeeMappings");

            migrationBuilder.DropTable(
                name: "AttendancePolicyMasters");
        }
    }
}
