using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KioskCenter.Migrations
{
    /// <inheritdoc />
    public partial class inininijijii : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkingHours",
                table: "RestaurantStyles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TextColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SecondaryColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "RestaurantName",
                table: "RestaurantStyles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "RestaurantStyles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Instagram",
                table: "RestaurantStyles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FooterText",
                table: "RestaurantStyles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FontWeight",
                table: "RestaurantStyles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FontStyle",
                table: "RestaurantStyles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FontName",
                table: "RestaurantStyles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FontFamily",
                table: "RestaurantStyles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CustomFontUrl",
                table: "RestaurantStyles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ButtonHoverColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ButtonColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "BackgroundColor",
                table: "RestaurantStyles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "RestaurantStyles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AnimationDuration",
                table: "RestaurantStyles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BackgroundImage",
                table: "RestaurantStyles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BackgroundOverlay",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ButtonBorderRadius",
                table: "RestaurantStyles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ButtonTextColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CardBorderRadius",
                table: "RestaurantStyles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategoryBtnActiveBgColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategoryBtnActiveTextColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategoryBtnBgColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategoryBtnTextColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EnableAnimations",
                table: "RestaurantStyles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FontSizeBase",
                table: "RestaurantStyles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FontSizeTitle",
                table: "RestaurantStyles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FooterBgColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FooterTextColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrderTypeCardBgColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrderTypeCardHoverColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrderTypeCardTextColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductCardBgColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductCardBorderRadius",
                table: "RestaurantStyles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductCardHoverColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SidebarBgColor",
                table: "RestaurantStyles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SidebarHeaderBgColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SidebarItemBgColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SidebarItemHoverColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpacingBase",
                table: "RestaurantStyles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TextSecondaryColor",
                table: "RestaurantStyles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "AnimationDuration",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "BackgroundImage",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "BackgroundOverlay",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "ButtonBorderRadius",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "ButtonTextColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "CardBorderRadius",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "CategoryBtnActiveBgColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "CategoryBtnActiveTextColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "CategoryBtnBgColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "CategoryBtnTextColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "EnableAnimations",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "FontSizeBase",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "FontSizeTitle",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "FooterBgColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "FooterTextColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "OrderTypeCardBgColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "OrderTypeCardHoverColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "OrderTypeCardTextColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "ProductCardBgColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "ProductCardBorderRadius",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "ProductCardHoverColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "SidebarBgColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "SidebarHeaderBgColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "SidebarItemBgColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "SidebarItemHoverColor",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "SpacingBase",
                table: "RestaurantStyles");

            migrationBuilder.DropColumn(
                name: "TextSecondaryColor",
                table: "RestaurantStyles");

            migrationBuilder.AlterColumn<string>(
                name: "WorkingHours",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "TextColor",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "SecondaryColor",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "RestaurantName",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryColor",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Instagram",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "FooterText",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "FontWeight",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "FontStyle",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "FontName",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FontFamily",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "CustomFontUrl",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ButtonHoverColor",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ButtonColor",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "BackgroundColor",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "RestaurantStyles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }
    }
}
