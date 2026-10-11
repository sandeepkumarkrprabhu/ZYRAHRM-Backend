using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZYRA.Attendance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdateModuleMenuPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "ModuleId", "Description", "IsActive", "ModuleCode", "ModuleName" },
                values: new object[] { 5, "Asset categories, assets, clients, vendors, and assignments", true, "ASSET", "Asset Management" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "PermissionCode", "PermissionName" },
                values: new object[,]
                {
                    { 8L, "Asset Management", "View asset category records", "ASSET_CATEGORY_VIEW", "View Asset Categories" },
                    { 9L, "Asset Management", "Create and update asset categories", "ASSET_CATEGORY_MANAGE", "Manage Asset Categories" },
                    { 10L, "Asset Management", "View asset master records", "ASSET_VIEW", "View Assets" },
                    { 11L, "Asset Management", "Create and update asset master records", "ASSET_MANAGE", "Manage Assets" },
                    { 12L, "Asset Management", "View client master records", "CLIENT_VIEW", "View Clients" },
                    { 13L, "Asset Management", "Create and update client master records", "CLIENT_MANAGE", "Manage Clients" },
                    { 14L, "Asset Management", "View vendor master records", "VENDOR_VIEW", "View Vendors" },
                    { 15L, "Asset Management", "Create and update vendor master records", "VENDOR_MANAGE", "Manage Vendors" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 14, 8, 1 },
                    { 15, 9, 1 },
                    { 16, 10, 1 },
                    { 17, 11, 1 },
                    { 18, 12, 1 },
                    { 19, 13, 1 },
                    { 20, 14, 1 },
                    { 21, 15, 1 },
                    { 22, 8, 2 },
                    { 23, 9, 2 },
                    { 24, 10, 2 },
                    { 25, 11, 2 },
                    { 26, 12, 2 },
                    { 27, 13, 2 },
                    { 28, 14, 2 },
                    { 29, 15, 2 },
                    { 30, 8, 5 },
                    { 31, 9, 5 },
                    { 32, 10, 5 },
                    { 33, 11, 5 },
                    { 34, 12, 5 },
                    { 35, 13, 5 },
                    { 36, 14, 5 },
                    { 37, 15, 5 },
                    { 38, 8, 6 },
                    { 39, 9, 6 },
                    { 40, 10, 6 },
                    { 41, 11, 6 },
                    { 42, 12, 6 },
                    { 43, 13, 6 },
                    { 44, 14, 6 },
                    { 45, 15, 6 },
                    { 46, 8, 4 },
                    { 47, 10, 4 },
                    { 48, 12, 4 },
                    { 49, 14, 4 }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedDateTime", "Description", "IsActive", "RoleName", "UpdatedDateTime" },
                values: new object[,]
                {
                    { 5L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Manage IT assets, asset categories, clients, vendors, and asset lifecycle", false, "IT Admin", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Maintain asset records, categories, client records, and vendor records", false, "Asset Manager", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "NavigationMenus",
                columns: new[] { "MenuId", "Description", "DisplayOrder", "IconName", "MenuKey", "MenuLabel", "ModuleId", "RequiredPermissionCode" },
                values: new object[,]
                {
                    { 9, "Manage categories used to classify company and client assets", 1, "Tags", "asset-cat", "Asset Categories", 5, "ASSET_CATEGORY_VIEW" },
                    { 10, "Register and manage owned, rented, and client-owned assets", 2, "Laptop", "assets", "Asset Master", 5, "ASSET_VIEW" },
                    { 11, "Manage client records used for client-owned assets", 3, "Building2", "clients", "Client Master", 5, "CLIENT_VIEW" },
                    { 12, "Manage vendors supplying rented assets and services", 4, "Truck", "vendors", "Vendor Master", 5, "VENDOR_VIEW" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "NavigationMenus",
                keyColumn: "MenuId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 8L);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 9L);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 10L);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 11L);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 12L);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 13L);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 14L);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 15L);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6L);

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "ModuleId",
                keyValue: 5);
        }
    }
}
