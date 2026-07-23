using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusAs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllianceCommissionToPartnerConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AllianceCommissionPercent",
                table: "PartnerConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllianceCommissionPercent",
                table: "PartnerConfigs");

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 22, 48, 43, 310, DateTimeKind.Local).AddTicks(8315));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 22, 48, 43, 310, DateTimeKind.Local).AddTicks(8341));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 22, 48, 43, 310, DateTimeKind.Local).AddTicks(8343));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 22, 48, 43, 310, DateTimeKind.Local).AddTicks(8346));

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 22, 48, 43, 310, DateTimeKind.Local).AddTicks(8348));
        }
    }
}
