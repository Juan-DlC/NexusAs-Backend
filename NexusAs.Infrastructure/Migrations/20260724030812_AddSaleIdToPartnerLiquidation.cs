using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusAs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleIdToPartnerLiquidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SaleId",
                table: "PartnerLiquidations",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 23, 22, 8, 11, 828, DateTimeKind.Local).AddTicks(9208));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 23, 22, 8, 11, 828, DateTimeKind.Local).AddTicks(9248));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 23, 22, 8, 11, 828, DateTimeKind.Local).AddTicks(9254));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 23, 22, 8, 11, 828, DateTimeKind.Local).AddTicks(9259));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 23, 22, 8, 11, 828, DateTimeKind.Local).AddTicks(9264));

            migrationBuilder.CreateIndex(
                name: "IX_PartnerLiquidations_SaleId",
                table: "PartnerLiquidations",
                column: "SaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartnerLiquidations_Sales_SaleId",
                table: "PartnerLiquidations",
                column: "SaleId",
                principalTable: "Sales",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartnerLiquidations_Sales_SaleId",
                table: "PartnerLiquidations");

            migrationBuilder.DropIndex(
                name: "IX_PartnerLiquidations_SaleId",
                table: "PartnerLiquidations");

            migrationBuilder.DropColumn(
                name: "SaleId",
                table: "PartnerLiquidations");

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 22, 22, 20, 19, 361, DateTimeKind.Local).AddTicks(1058));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 22, 22, 20, 19, 361, DateTimeKind.Local).AddTicks(1075));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 22, 22, 20, 19, 361, DateTimeKind.Local).AddTicks(1077));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 22, 22, 20, 19, 361, DateTimeKind.Local).AddTicks(1080));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 22, 22, 20, 19, 361, DateTimeKind.Local).AddTicks(1082));
        }
    }
}
