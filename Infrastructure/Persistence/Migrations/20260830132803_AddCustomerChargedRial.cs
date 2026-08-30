using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SamanMobileInsurance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerChargedRial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CustomerChargedRial",
                table: "InsurancePolicies",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE InsurancePolicies SET CustomerChargedRial = PremiumRial");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerChargedRial",
                table: "InsurancePolicies");
        }
    }
}
