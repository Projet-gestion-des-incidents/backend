using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projet0.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCommercantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TPEs_AspNetUsers_CommercantId",
                table: "TPEs");

            migrationBuilder.AlterColumn<Guid>(
                name: "CommercantId",
                table: "TPEs",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_TPEs_AspNetUsers_CommercantId",
                table: "TPEs",
                column: "CommercantId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TPEs_AspNetUsers_CommercantId",
                table: "TPEs");

            migrationBuilder.AlterColumn<Guid>(
                name: "CommercantId",
                table: "TPEs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TPEs_AspNetUsers_CommercantId",
                table: "TPEs",
                column: "CommercantId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
