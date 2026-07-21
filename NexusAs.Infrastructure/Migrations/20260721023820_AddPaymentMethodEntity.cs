using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NexusAs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethodId",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PaymentMethods",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "CASH", new DateTime(2026, 7, 20, 21, 38, 19, 645, DateTimeKind.Local).AddTicks(1060), "Pago en efectivo", true, "Contado", null },
                    { 2, "CREDIT", new DateTime(2026, 7, 20, 21, 38, 19, 645, DateTimeKind.Local).AddTicks(1072), "Pago a crédito", true, "Crédito", null },
                    { 3, "ADDI", new DateTime(2026, 7, 20, 21, 38, 19, 645, DateTimeKind.Local).AddTicks(1074), "Pago mediante plataforma Addi", true, "Addi", null },
                    { 4, "SISTECREDITO", new DateTime(2026, 7, 20, 21, 38, 19, 645, DateTimeKind.Local).AddTicks(1075), "Pago mediante Sistecredito", true, "Sistecredito", null },
                    { 5, "CARD", new DateTime(2026, 7, 20, 21, 38, 19, 645, DateTimeKind.Local).AddTicks(1076), "Pago con tarjeta de crédito o débito", true, "Tarjeta", null }
                });

            // Migrar datos existentes del enum a la nueva FK
            // PaymentMethod enum convertido a string: Cash, Credit
            migrationBuilder.Sql(@"
                UPDATE Sales 
                SET PaymentMethodId = CASE 
                    WHEN PaymentMethod = 'Cash' THEN 1  -- Cash -> CASH
                    WHEN PaymentMethod = 'Credit' THEN 2  -- Credit -> CREDIT
                    ELSE 1  -- Default a Cash por seguridad
                END
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_PaymentMethodId",
                table: "Sales",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_Code",
                table: "PaymentMethods",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_PaymentMethods_PaymentMethodId",
                table: "Sales",
                column: "PaymentMethodId",
                principalTable: "PaymentMethods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_PaymentMethods_PaymentMethodId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_Sales_PaymentMethodId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PaymentMethodId",
                table: "Sales");
        }
    }
}
