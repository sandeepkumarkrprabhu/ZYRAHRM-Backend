using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZYRA.Attendance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetDashboardMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "NavigationMenus",
                columns: new[] { "MenuId", "Description", "DisplayOrder", "IconName", "MenuKey", "MenuLabel", "ModuleId", "RequiredPermissionCode" },
                values: new object[] { 13, "Overview of asset inventory, assignments, categories, clients, and vendors", 1, "LayoutDashboard", "asset-dash", "Dashboard", 5, "ASSET_VIEW" });

            migrationBuilder.UpdateData(
                table: "NavigationMenus", keyColumn: "MenuId", keyValue: 9,
                column: "DisplayOrder", value: 2);
            migrationBuilder.UpdateData(
                table: "NavigationMenus", keyColumn: "MenuId", keyValue: 10,
                column: "DisplayOrder", value: 3);
            migrationBuilder.UpdateData(
                table: "NavigationMenus", keyColumn: "MenuId", keyValue: 11,
                column: "DisplayOrder", value: 4);
            migrationBuilder.UpdateData(
                table: "NavigationMenus", keyColumn: "MenuId", keyValue: 12,
                column: "DisplayOrder", value: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NavigationMenus", keyColumn: "MenuId", keyValue: 13);

            migrationBuilder.UpdateData(
                table: "NavigationMenus", keyColumn: "MenuId", keyValue: 9,
                column: "DisplayOrder", value: 1);
            migrationBuilder.UpdateData(
                table: "NavigationMenus", keyColumn: "MenuId", keyValue: 10,
                column: "DisplayOrder", value: 2);
            migrationBuilder.UpdateData(
                table: "NavigationMenus", keyColumn: "MenuId", keyValue: 11,
                column: "DisplayOrder", value: 3);
            migrationBuilder.UpdateData(
                table: "NavigationMenus", keyColumn: "MenuId", keyValue: 12,
                column: "DisplayOrder", value: 4);
        }
    }
}
