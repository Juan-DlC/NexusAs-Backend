using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusAs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedByAndRequestIdToSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcessedByUserId",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestId",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 14, 13, 41, 47, 762, DateTimeKind.Local).AddTicks(5745));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 14, 13, 41, 47, 762, DateTimeKind.Local).AddTicks(5763));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 14, 13, 41, 47, 762, DateTimeKind.Local).AddTicks(5765));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 14, 13, 41, 47, 762, DateTimeKind.Local).AddTicks(5767));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 14, 13, 41, 47, 762, DateTimeKind.Local).AddTicks(5768));

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ProcessedByUserId",
                table: "Sales",
                column: "ProcessedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Users_ProcessedByUserId",
                table: "Sales",
                column: "ProcessedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Users_ProcessedByUserId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_ProcessedByUserId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Sales");

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
    }
}
