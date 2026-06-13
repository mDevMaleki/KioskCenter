using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KioskCenter.Migrations
{
    /// <inheritdoc />
    public partial class ininininiPP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomFontUrl",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FontName",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FontStyle",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FontWeight",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomFontUrl",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "FontName",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "FontStyle",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "FontWeight",
                table: "RestaurantStyles");
        }
    }
}
