using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorDeFinancas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseInstallmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InstallmentGroupId",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentNumber",
                table: "Purchases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalInstallments",
                table: "Purchases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_InstallmentGroupId",
                table: "Purchases",
                column: "InstallmentGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_InstallmentGroupId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "InstallmentGroupId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "InstallmentNumber",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "TotalInstallments",
                table: "Purchases");
        }
    }
}
