using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tenants");
        }
    }
}
