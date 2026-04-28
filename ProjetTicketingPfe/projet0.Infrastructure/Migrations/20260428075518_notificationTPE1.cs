using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projet0.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class notificationTPE1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_TPEs_TPEId",
                table: "Notifications");

            migrationBuilder.AddColumn<Guid>(
                name: "TPEId1",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TPEId1",
                table: "Notifications",
                column: "TPEId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_TPEs_TPEId",
                table: "Notifications",
                column: "TPEId",
                principalTable: "TPEs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_TPEs_TPEId1",
                table: "Notifications",
                column: "TPEId1",
                principalTable: "TPEs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_TPEs_TPEId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_TPEs_TPEId1",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TPEId1",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TPEId1",
                table: "Notifications");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_TPEs_TPEId",
                table: "Notifications",
                column: "TPEId",
                principalTable: "TPEs",
                principalColumn: "Id");
        }
    }
}
