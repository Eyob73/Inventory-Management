using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentDestinationAndVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentDestinations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    BankCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BankName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    AccountNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    AccountHolderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDestinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentDestinations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PaymentVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentDestinationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BankCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubmittedReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NormalizedReference = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    VerifiedReference = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ExpectedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VerifiedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DestinationMatchStatus = table.Column<int>(type: "integer", nullable: false),
                    PayerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PayerAccount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReceiverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReceiverAccount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TransactionDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    VerifiedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProviderTransactionId = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsManualReviewApproved = table.Column<bool>(type: "boolean", nullable: true),
                    ManualReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReviewedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentVerifications_PaymentDestinations_PaymentDestination~",
                        column: x => x.PaymentDestinationId,
                        principalTable: "PaymentDestinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PaymentVerifications_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDestinations_CreatedByUserId",
                table: "PaymentDestinations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDestinations_TenantId_IsActive",
                table: "PaymentDestinations",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDestinations_TenantId_IsDefault",
                table: "PaymentDestinations",
                columns: new[] { "TenantId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerifications_PaymentDestinationId",
                table: "PaymentVerifications",
                column: "PaymentDestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerifications_SaleId",
                table: "PaymentVerifications",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerifications_TenantId_BankCode_NormalizedReference",
                table: "PaymentVerifications",
                columns: new[] { "TenantId", "BankCode", "NormalizedReference" },
                unique: true,
                filter: "\"Status\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerifications_TenantId_CreatedAtUtc",
                table: "PaymentVerifications",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerifications_TenantId_Status",
                table: "PaymentVerifications",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentVerifications");

            migrationBuilder.DropTable(
                name: "PaymentDestinations");
        }
    }
}
