using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KioskCenter.Migrations
{
    /// <inheritdoc />
    public partial class AddMoadianIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MoadianError",
                table: "SaleInvoices",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoadianReferenceNumber",
                table: "SaleInvoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MoadianSent",
                table: "SaleInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MoadianSentAt",
                table: "SaleInvoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoadianTaxId",
                table: "SaleInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EconomicCode",
                table: "Parties",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MoadianSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PublicKeyPem = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PrivateKeyPem = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OrgKeyId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BaseUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoadianSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MoadianSettings");

            migrationBuilder.DropColumn(
                name: "MoadianError",
                table: "SaleInvoices");

            migrationBuilder.DropColumn(
                name: "MoadianReferenceNumber",
                table: "SaleInvoices");

            migrationBuilder.DropColumn(
                name: "MoadianSent",
                table: "SaleInvoices");

            migrationBuilder.DropColumn(
                name: "MoadianSentAt",
                table: "SaleInvoices");

            migrationBuilder.DropColumn(
                name: "MoadianTaxId",
                table: "SaleInvoices");

            migrationBuilder.DropColumn(
                name: "EconomicCode",
                table: "Parties");
        }
    }
}
