using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusAs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountPercentToSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 12, 0, 38, 50, 974, DateTimeKind.Local).AddTicks(1331));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 12, 0, 38, 50, 974, DateTimeKind.Local).AddTicks(1355));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 12, 0, 38, 50, 974, DateTimeKind.Local).AddTicks(1357));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 12, 0, 38, 50, 974, DateTimeKind.Local).AddTicks(1359));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 12, 0, 38, 50, 974, DateTimeKind.Local).AddTicks(1362));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Sales");

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 1, 14, 17, 39, 85, DateTimeKind.Local).AddTicks(9972));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 1, 14, 17, 39, 86, DateTimeKind.Local).AddTicks(6));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 1, 14, 17, 39, 86, DateTimeKind.Local).AddTicks(8));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 1, 14, 17, 39, 86, DateTimeKind.Local).AddTicks(10));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 1, 14, 17, 39, 86, DateTimeKind.Local).AddTicks(12));
        }
    }
}
