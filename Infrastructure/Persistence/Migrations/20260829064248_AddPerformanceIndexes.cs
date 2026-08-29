using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SamanMobileInsurance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_PolicyId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_MobileModels_BrandId",
                table: "MobileModels");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_Imei1",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_Imei2",
                table: "InsurancePolicies");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_IsActive",
                table: "Stores",
                column: "IsActive",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SalesFestivals_IsActive_StartsAt_EndsAt",
                table: "SalesFestivals",
                columns: new[] { "IsActive", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAt",
                table: "Payments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PolicyId_Status",
                table: "Payments",
                columns: new[] { "PolicyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MobileModels_BrandId_IsActive_Name",
                table: "MobileModels",
                columns: new[] { "BrandId", "IsActive", "Name" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MobileBrands_IsActive_Name",
                table: "MobileBrands",
                columns: new[] { "IsActive", "Name" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_Imei1_Status",
                table: "InsurancePolicies",
                columns: new[] { "Imei1", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_Imei2_Status",
                table: "InsurancePolicies",
                columns: new[] { "Imei2", "Status" },
                filter: "[Imei2] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_Status_IssueDate",
                table: "InsurancePolicies",
                columns: new[] { "Status", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_StoreId_CreatedAt",
                table: "InsurancePolicies",
                columns: new[] { "StoreId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_StoreId_Status_EndDate",
                table: "InsurancePolicies",
                columns: new[] { "StoreId", "Status", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_StoreId_Status_IssueDate",
                table: "InsurancePolicies",
                columns: new[] { "StoreId", "Status", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CreatedAt",
                table: "Customers",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Stores_IsActive",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_SalesFestivals_IsActive_StartsAt_EndsAt",
                table: "SalesFestivals");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CreatedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PolicyId_Status",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_MobileModels_BrandId_IsActive_Name",
                table: "MobileModels");

            migrationBuilder.DropIndex(
                name: "IX_MobileBrands_IsActive_Name",
                table: "MobileBrands");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_Imei1_Status",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_Imei2_Status",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_Status_IssueDate",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_StoreId_CreatedAt",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_StoreId_Status_EndDate",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_StoreId_Status_IssueDate",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CreatedAt",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PolicyId",
                table: "Payments",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_MobileModels_BrandId",
                table: "MobileModels",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_Imei1",
                table: "InsurancePolicies",
                column: "Imei1");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_Imei2",
                table: "InsurancePolicies",
                column: "Imei2");
        }
    }
}
