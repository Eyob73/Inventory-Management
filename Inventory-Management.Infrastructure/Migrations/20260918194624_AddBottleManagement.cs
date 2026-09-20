using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBottleManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalBottleDeposit",
                table: "Sales",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BottleDepositAmount",
                table: "SaleItems",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "BottleTypeId",
                table: "SaleItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBottleExchange",
                table: "SaleItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "BottleTypeId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReturnable",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BottleTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Capacity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Material = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BottleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BottleInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BottleTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullBottles = table.Column<int>(type: "integer", nullable: false),
                    EmptyBottles = table.Column<int>(type: "integer", nullable: false),
                    DamagedBottles = table.Column<int>(type: "integer", nullable: false),
                    LostBottles = table.Column<int>(type: "integer", nullable: false),
                    WithCustomers = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BottleInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BottleInventories_BottleTypes_BottleTypeId",
                        column: x => x.BottleTypeId,
                        principalTable: "BottleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BottleTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BottleTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BottleTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BottleTransactions_BottleTypes_BottleTypeId",
                        column: x => x.BottleTypeId,
                        principalTable: "BottleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BottleTransactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerBottleBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BottleTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<int>(type: "integer", nullable: false),
                    TotalDeposit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBottleBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerBottleBalances_BottleTypes_BottleTypeId",
                        column: x => x.BottleTypeId,
                        principalTable: "BottleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerBottleBalances_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_BottleTypeId",
                table: "Products",
                column: "BottleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BottleInventories_BottleTypeId",
                table: "BottleInventories",
                column: "BottleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BottleTransactions_BottleTypeId",
                table: "BottleTransactions",
                column: "BottleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BottleTransactions_CustomerId",
                table: "BottleTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBottleBalances_BottleTypeId",
                table: "CustomerBottleBalances",
                column: "BottleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBottleBalances_CustomerId",
                table: "CustomerBottleBalances",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_BottleTypes_BottleTypeId",
                table: "Products",
                column: "BottleTypeId",
                principalTable: "BottleTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_BottleTypes_BottleTypeId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "BottleInventories");

            migrationBuilder.DropTable(
                name: "BottleTransactions");

            migrationBuilder.DropTable(
                name: "CustomerBottleBalances");

            migrationBuilder.DropTable(
                name: "BottleTypes");

            migrationBuilder.DropIndex(
                name: "IX_Products_BottleTypeId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TotalBottleDeposit",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "BottleDepositAmount",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "BottleTypeId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "IsBottleExchange",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "BottleTypeId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsReturnable",
                table: "Products");
        }
    }
}
