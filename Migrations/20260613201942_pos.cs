using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KioskCenter.Migrations
{
    /// <inheritdoc />
    public partial class pos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // مقادیر متنی قبلی (Parsian/PardakhtNovin) باید قبل از تغییر نوع ستون
            // به معادل عددی enum (PardakhtNovin=0, Parsian=1) تبدیل شوند
            migrationBuilder.Sql(@"
                UPDATE PosDevices
                SET [Type] = CASE [Type]
                    WHEN 'Parsian' THEN '1'
                    WHEN 'PardakhtNovin' THEN '0'
                    ELSE '0'
                END");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "PosDevices",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "PosDevices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql(@"
                UPDATE PosDevices
                SET [Type] = CASE [Type]
                    WHEN '1' THEN 'Parsian'
                    WHEN '0' THEN 'PardakhtNovin'
                    ELSE [Type]
                END");
        }
    }
}
