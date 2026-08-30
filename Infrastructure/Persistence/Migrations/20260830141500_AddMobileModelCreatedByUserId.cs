using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SamanMobileInsurance.Infrastructure.Persistence;

#nullable disable

namespace SamanMobileInsurance.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260830141500_AddMobileModelCreatedByUserId")]
    public partial class AddMobileModelCreatedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "MobileModels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MobileModels_CreatedByUserId",
                table: "MobileModels",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MobileModels_CreatedByUserId",
                table: "MobileModels");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MobileModels");
        }
    }
}
