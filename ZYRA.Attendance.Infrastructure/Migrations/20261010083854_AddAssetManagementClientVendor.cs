using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZYRA.Attendance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetManagementClientVendor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                schema: "asset",
                table: "Assets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceAdminAccountName",
                schema: "asset",
                table: "Assets",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceAdminAccountNotes",
                schema: "asset",
                table: "Assets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceAdminCredentialSecretReference",
                schema: "asset",
                table: "Assets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcurementReference",
                schema: "asset",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentalCost",
                schema: "asset",
                table: "Assets",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentalCostFrequency",
                schema: "asset",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RentalEndDate",
                schema: "asset",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RentalStartDate",
                schema: "asset",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VendorId",
                schema: "asset",
                table: "Assets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClientMaster",
                schema: "asset",
                columns: table => new
                {
                    ClientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientMaster", x => x.ClientId);
                });

            migrationBuilder.CreateTable(
                name: "VendorMaster",
                schema: "asset",
                columns: table => new
                {
                    VendorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TaxRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorMaster", x => x.VendorId);
                });

            migrationBuilder.CreateTable(
                name: "ClientEmployeeAssignments",
                schema: "asset",
                columns: table => new
                {
                    ClientEmployeeAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    EmployeeMappingId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientEmployeeAssignments", x => x.ClientEmployeeAssignmentId);
                    table.ForeignKey(
                        name: "FK_ClientEmployeeAssignments_ClientMaster_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "asset",
                        principalTable: "ClientMaster",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientEmployeeAssignments_EmployeeMappings_EmployeeMappingId",
                        column: x => x.EmployeeMappingId,
                        principalTable: "EmployeeMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ClientId",
                schema: "asset",
                table: "Assets",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_VendorId",
                schema: "asset",
                table: "Assets",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientEmployeeAssignments_Client_Employee_EffectiveTo",
                schema: "asset",
                table: "ClientEmployeeAssignments",
                columns: new[] { "ClientId", "EmployeeMappingId", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientEmployeeAssignments_Employee_EffectiveTo",
                schema: "asset",
                table: "ClientEmployeeAssignments",
                columns: new[] { "EmployeeMappingId", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "UX_ClientEmployeeAssignments_OneActiveClientPerEmployeePair",
                schema: "asset",
                table: "ClientEmployeeAssignments",
                columns: new[] { "ClientId", "EmployeeMappingId" },
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClientMaster_ClientCode",
                schema: "asset",
                table: "ClientMaster",
                column: "ClientCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorMaster_VendorCode",
                schema: "asset",
                table: "VendorMaster",
                column: "VendorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorMaster_VendorName",
                schema: "asset",
                table: "VendorMaster",
                column: "VendorName");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_ClientMaster_ClientId",
                schema: "asset",
                table: "Assets",
                column: "ClientId",
                principalSchema: "asset",
                principalTable: "ClientMaster",
                principalColumn: "ClientId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_VendorMaster_VendorId",
                schema: "asset",
                table: "Assets",
                column: "VendorId",
                principalSchema: "asset",
                principalTable: "VendorMaster",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_ClientMaster_ClientId",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropForeignKey(
                name: "FK_Assets_VendorMaster_VendorId",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropTable(
                name: "ClientEmployeeAssignments",
                schema: "asset");

            migrationBuilder.DropTable(
                name: "VendorMaster",
                schema: "asset");

            migrationBuilder.DropTable(
                name: "ClientMaster",
                schema: "asset");

            migrationBuilder.DropIndex(
                name: "IX_Assets_ClientId",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_VendorId",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ClientId",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeviceAdminAccountName",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeviceAdminAccountNotes",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeviceAdminCredentialSecretReference",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ProcurementReference",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "RentalCost",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "RentalCostFrequency",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "RentalEndDate",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "RentalStartDate",
                schema: "asset",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "VendorId",
                schema: "asset",
                table: "Assets");
        }
    }
}
