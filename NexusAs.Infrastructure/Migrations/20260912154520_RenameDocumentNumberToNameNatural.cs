using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusAs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameDocumentNumberToNameNatural : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessPartners_DocumentNumber_IsActive",
                table: "BusinessPartners");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "BusinessPartners");

            migrationBuilder.AddColumn<string>(
                name: "NameNatural",
                table: "BusinessPartners",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartners_NameNatural_IsActive",
                table: "BusinessPartners",
                columns: new[] { "NameNatural", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessPartners_NameNatural_IsActive",
                table: "BusinessPartners");

            migrationBuilder.DropColumn(
                name: "NameNatural",
                table: "BusinessPartners");

            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                table: "BusinessPartners",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartners_DocumentNumber_IsActive",
                table: "BusinessPartners",
                columns: new[] { "DocumentNumber", "IsActive" });
        }
    }
}
