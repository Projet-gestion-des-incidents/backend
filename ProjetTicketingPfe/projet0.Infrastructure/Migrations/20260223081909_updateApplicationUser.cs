using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projet0.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_AssigneeId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_CreateurId",
                table: "Tickets");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceTicket",
                table: "Tickets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationUserId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationUserId1",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationUserId",
                table: "PiecesJointes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationUserId",
                table: "CommentairesTicket",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ApplicationUserId",
                table: "Tickets",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ApplicationUserId1",
                table: "Tickets",
                column: "ApplicationUserId1");

            migrationBuilder.CreateIndex(
                name: "IX_PiecesJointes_ApplicationUserId",
                table: "PiecesJointes",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentairesTicket_ApplicationUserId",
                table: "CommentairesTicket",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommentairesTicket_AspNetUsers_ApplicationUserId",
                table: "CommentairesTicket",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PiecesJointes_AspNetUsers_ApplicationUserId",
                table: "PiecesJointes",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_ApplicationUserId",
                table: "Tickets",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_ApplicationUserId1",
                table: "Tickets",
                column: "ApplicationUserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Assignee",
                table: "Tickets",
                column: "AssigneeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Createur",
                table: "Tickets",
                column: "CreateurId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentairesTicket_AspNetUsers_ApplicationUserId",
                table: "CommentairesTicket");

            migrationBuilder.DropForeignKey(
                name: "FK_PiecesJointes_AspNetUsers_ApplicationUserId",
                table: "PiecesJointes");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_ApplicationUserId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_ApplicationUserId1",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Assignee",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Createur",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ApplicationUserId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ApplicationUserId1",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_PiecesJointes_ApplicationUserId",
                table: "PiecesJointes");

            migrationBuilder.DropIndex(
                name: "IX_CommentairesTicket_ApplicationUserId",
                table: "CommentairesTicket");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId1",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "PiecesJointes");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "CommentairesTicket");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceTicket",
                table: "Tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_AssigneeId",
                table: "Tickets",
                column: "AssigneeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_CreateurId",
                table: "Tickets",
                column: "CreateurId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
