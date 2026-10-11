using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZYRA.Attendance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleManagementReviewUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ModuleId",
                table: "NavigationMenus",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    ModuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.ModuleId);
                });

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "ModuleId", "Description", "IsActive", "ModuleCode", "ModuleName" },
                values: new object[,]
                {
                    { 1, "Employee and HR administration", true, "HRM", "Human Resource Management" },
                    { 2, "Attendance policies and reporting", true, "ATTENDANCE", "Attendance Management" },
                    { 3, "Biometric synchronization and integrations", true, "INTEGRATION", "Integration Management" },
                    { 4, "Audit and global system configuration", true, "ADMIN", "System Administration" }
                });

            migrationBuilder.UpdateData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 1,
                column: "ModuleId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 2,
                column: "ModuleId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 3,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 4,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 5,
                column: "ModuleId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 7,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 8,
                column: "ModuleId",
                value: 2);

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenus_ModuleId",
                table: "NavigationMenus",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_ModuleCode",
                table: "Modules",
                column: "ModuleCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NavigationMenus_Modules_ModuleId",
                table: "NavigationMenus",
                column: "ModuleId",
                principalTable: "Modules",
                principalColumn: "ModuleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NavigationMenus_Modules_ModuleId",
                table: "NavigationMenus");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_NavigationMenus_ModuleId",
                table: "NavigationMenus");

            migrationBuilder.DropColumn(
                name: "ModuleId",
                table: "NavigationMenus");
        }
    }
}
