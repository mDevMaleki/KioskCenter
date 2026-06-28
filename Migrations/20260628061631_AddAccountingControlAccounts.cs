using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KioskCenter.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingControlAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "Balance", "Code", "IsActive", "IsGroup", "Name", "ParentId", "Type" },
                values: new object[,]
                {
                    { 15, 0m, "1210", true, false, "اسناد دریافتنی (چک)", 1, 1 },
                    { 16, 0m, "2200", true, false, "اسناد پرداختنی (چک)", 5, 2 },
                    { 21, 0m, "1400", true, false, "دارایی‌های ثابت", 1, 1 },
                    { 22, 0m, "1410", true, false, "استهلاک انباشته دارایی ثابت", 1, 1 },
                    { 23, 0m, "1500", true, false, "تنخواه‌گردان", 1, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 23);
        }
    }
}
