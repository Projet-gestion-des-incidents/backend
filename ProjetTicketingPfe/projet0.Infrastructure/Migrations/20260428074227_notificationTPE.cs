using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projet0.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class notificationTPE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TPEId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TPEId",
                table: "Notifications",
                column: "TPEId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_TPEs_TPEId",
                table: "Notifications",
                column: "TPEId",
                principalTable: "TPEs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_TPEs_TPEId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TPEId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TPEId",
                table: "Notifications");
        }
    }
}
