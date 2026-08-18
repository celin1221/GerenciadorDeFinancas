using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorDeFinancas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationButtons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationButtons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationButtons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationButtonPersons",
                columns: table => new
                {
                    ButtonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationButtonPersons", x => new { x.ButtonId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_NotificationButtonPersons_NotificationButtons_ButtonId",
                        column: x => x.ButtonId,
                        principalTable: "NotificationButtons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationButtonPersons_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationButtonPersons_PersonId",
                table: "NotificationButtonPersons",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationButtonPersons");

            migrationBuilder.DropTable(
                name: "NotificationButtons");
        }
    }
}
