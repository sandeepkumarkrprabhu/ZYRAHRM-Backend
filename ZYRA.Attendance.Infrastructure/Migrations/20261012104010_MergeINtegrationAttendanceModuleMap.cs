using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZYRA.Attendance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeINtegrationAttendanceModuleMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "ModuleId",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 5,
                columns: new[] { "Description", "ModuleId" },
                values: new object[] { "Attendance synchronization API and Hangfire queue monitor", 2 });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 6L,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Attendance", "Execute the attendance synchronization background job" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "ModuleId", "Description", "IsActive", "ModuleCode", "ModuleName" },
                values: new object[] { 3, "Biometric synchronization and integrations", true, "INTEGRATION", "Integration Management" });

            migrationBuilder.UpdateData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 5,
                columns: new[] { "Description", "ModuleId" },
                values: new object[] { "Single API contract and Hangfire queue monitor", 3 });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 6L,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Integration", "Execute ProcessAttendanceSyncJob background job" });
        }
    }
}
