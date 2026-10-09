using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZYRA.Attendance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetManagementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "asset");

            migrationBuilder.CreateTable(
                name: "AssetCategories",
                schema: "asset",
                columns: table => new
                {
                    AssetCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCategories", x => x.AssetCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                schema: "asset",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssetCategoryId = table.Column<int>(type: "int", nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModelNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OwnershipType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurchaseCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WarrantyExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.AssetId);
                    table.ForeignKey(
                        name: "FK_Assets_AssetCategories_AssetCategoryId",
                        column: x => x.AssetCategoryId,
                        principalSchema: "asset",
                        principalTable: "AssetCategories",
                        principalColumn: "AssetCategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetAssignments",
                schema: "asset",
                columns: table => new
                {
                    AssetAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    EmployeeMappingId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignmentRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReturnRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReturnedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAssignments", x => x.AssetAssignmentId);
                    table.ForeignKey(
                        name: "FK_AssetAssignments_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "asset",
                        principalTable: "Assets",
                        principalColumn: "AssetId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetAssignments_EmployeeMappings_EmployeeMappingId",
                        column: x => x.EmployeeMappingId,
                        principalSchema: "asset",
                        principalTable: "EmployeeMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetStatusHistory",
                schema: "asset",
                columns: table => new
                {
                    AssetStatusHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetStatusHistory", x => x.AssetStatusHistoryId);
                    table.ForeignKey(
                        name: "FK_AssetStatusHistory_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "asset",
                        principalTable: "Assets",
                        principalColumn: "AssetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_AssetId_ReturnedAt",
                schema: "asset",
                table: "AssetAssignments",
                columns: new[] { "AssetId", "ReturnedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_EmployeeMappingId",
                schema: "asset",
                table: "AssetAssignments",
                column: "EmployeeMappingId");

            migrationBuilder.CreateIndex(
                name: "UX_AssetAssignments_OneActiveAssignmentPerAsset",
                schema: "asset",
                table: "AssetAssignments",
                column: "AssetId",
                unique: true,
                filter: "[ReturnedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssetCategories_CategoryName",
                schema: "asset",
                table: "AssetCategories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetCategoryId",
                schema: "asset",
                table: "Assets",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetCode",
                schema: "asset",
                table: "Assets",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_SerialNumber",
                schema: "asset",
                table: "Assets",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AssetStatusHistory_AssetId_ChangedAt",
                schema: "asset",
                table: "AssetStatusHistory",
                columns: new[] { "AssetId", "ChangedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetAssignments",
                schema: "asset");

            migrationBuilder.DropTable(
                name: "AssetStatusHistory",
                schema: "asset");

            migrationBuilder.DropTable(
                name: "Assets",
                schema: "asset");

            migrationBuilder.DropTable(
                name: "AssetCategories",
                schema: "asset");
        }
    }
}
