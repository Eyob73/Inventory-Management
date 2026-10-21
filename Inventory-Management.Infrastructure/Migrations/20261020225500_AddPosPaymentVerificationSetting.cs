using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPosPaymentVerificationSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PosPaymentVerificationEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosPaymentVerificationEnabled",
                table: "Tenants");
        }
    }
}
