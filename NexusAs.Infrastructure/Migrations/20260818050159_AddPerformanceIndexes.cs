using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusAs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 18, 0, 1, 57, 876, DateTimeKind.Local).AddTicks(7660));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 18, 0, 1, 57, 876, DateTimeKind.Local).AddTicks(7690));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 18, 0, 1, 57, 876, DateTimeKind.Local).AddTicks(7692));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 18, 0, 1, 57, 876, DateTimeKind.Local).AddTicks(7695));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 18, 0, 1, 57, 876, DateTimeKind.Local).AddTicks(7697));

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Date",
                table: "Sales",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_UserId_Date",
                table: "Sales",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_CategoryId",
                table: "Products",
                columns: new[] { "IsActive", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerSales_PartnerConfigId_IsActive",
                table: "PartnerSales",
                columns: new[] { "PartnerConfigId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Credits_Status",
                table: "Credits",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_Date",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_UserId_Date",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PartnerSales_PartnerConfigId_IsActive",
                table: "PartnerSales");

            migrationBuilder.DropIndex(
                name: "IX_Credits_Status",
                table: "Credits");

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
        }
    }
}
