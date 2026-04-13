using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projet0.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class champsTPE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TPEs",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "TPEs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TPEs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "TPEs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TPEs_CreatedById",
                table: "TPEs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TPEs_UpdatedById",
                table: "TPEs",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_TPEs_AspNetUsers_CreatedById",
                table: "TPEs",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TPEs_AspNetUsers_UpdatedById",
                table: "TPEs",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TPEs_AspNetUsers_CreatedById",
                table: "TPEs");

            migrationBuilder.DropForeignKey(
                name: "FK_TPEs_AspNetUsers_UpdatedById",
                table: "TPEs");

            migrationBuilder.DropIndex(
                name: "IX_TPEs_CreatedById",
                table: "TPEs");

            migrationBuilder.DropIndex(
                name: "IX_TPEs_UpdatedById",
                table: "TPEs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TPEs");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "TPEs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TPEs");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "TPEs");
        }
    }
}
