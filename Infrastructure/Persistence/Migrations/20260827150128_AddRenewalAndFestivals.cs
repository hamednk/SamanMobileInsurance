using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SamanMobileInsurance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRenewalAndFestivals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDate",
                table: "InsurancePolicies",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RenewedFromPolicyId",
                table: "InsurancePolicies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE InsurancePolicies
                SET EndDate = DATEADD(year, 1, StartDate)
                WHERE Status = 5 AND EndDate IS NULL;
                """);

            migrationBuilder.CreateTable(
                name: "SalesFestivals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequiredIssuedCount = table.Column<int>(type: "int", nullable: false),
                    RewardText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesFestivals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_EndDate",
                table: "InsurancePolicies",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_RenewedFromPolicyId",
                table: "InsurancePolicies",
                column: "RenewedFromPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesFestivals_EndsAt",
                table: "SalesFestivals",
                column: "EndsAt");

            migrationBuilder.CreateIndex(
                name: "IX_SalesFestivals_IsActive",
                table: "SalesFestivals",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SalesFestivals_StartsAt",
                table: "SalesFestivals",
                column: "StartsAt");

            migrationBuilder.AddForeignKey(
                name: "FK_InsurancePolicies_InsurancePolicies_RenewedFromPolicyId",
                table: "InsurancePolicies",
                column: "RenewedFromPolicyId",
                principalTable: "InsurancePolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsurancePolicies_InsurancePolicies_RenewedFromPolicyId",
                table: "InsurancePolicies");

            migrationBuilder.DropTable(
                name: "SalesFestivals");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_EndDate",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_RenewedFromPolicyId",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "RenewedFromPolicyId",
                table: "InsurancePolicies");
        }
    }
}
