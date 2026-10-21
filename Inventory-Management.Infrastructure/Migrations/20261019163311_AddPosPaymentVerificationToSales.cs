using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPosPaymentVerificationToSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentVerifications_SaleId",
                table: "PaymentVerifications");

            migrationBuilder.AddColumn<bool>(
                name: "IsPaymentVerified",
                table: "Sales",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentVerificationId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentVerificationStatus",
                table: "Sales",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleId",
                table: "PaymentVerifications",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_PaymentVerificationId",
                table: "Sales",
                column: "PaymentVerificationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerifications_SaleId",
                table: "PaymentVerifications",
                column: "SaleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_PaymentVerifications_PaymentVerificationId",
                table: "Sales",
                column: "PaymentVerificationId",
                principalTable: "PaymentVerifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_PaymentVerifications_PaymentVerificationId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_PaymentVerificationId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_PaymentVerifications_SaleId",
                table: "PaymentVerifications");

            migrationBuilder.DropColumn(
                name: "IsPaymentVerified",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PaymentVerificationId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PaymentVerificationStatus",
                table: "Sales");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleId",
                table: "PaymentVerifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerifications_SaleId",
                table: "PaymentVerifications",
                column: "SaleId");
        }
    }
}
