using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projet0.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "IX_CommentairesTicket_ApplicationUserId",
                table: "CommentairesTicket");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId1",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PrioriteTicket",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CheminStockage",
                table: "PiecesJointes");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "PiecesJointes");

            migrationBuilder.DropColumn(
                name: "Taille",
                table: "PiecesJointes");

            migrationBuilder.DropColumn(
                name: "TypePieceJointe",
                table: "PiecesJointes");

            migrationBuilder.DropColumn(
                name: "TitreIncident",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "NouveauStatut",
                table: "HistoriquesTicket");

            migrationBuilder.DropColumn(
                name: "Nom",
                table: "EntitesImpactees");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "CommentairesTicket");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "PiecesJointes",
                newName: "IncidentId");

            migrationBuilder.RenameIndex(
                name: "IX_PiecesJointes_ApplicationUserId",
                table: "PiecesJointes",
                newName: "IX_PiecesJointes_IncidentId");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceTicket",
                table: "Tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateLimite",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Emplacement",
                table: "Incidents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TypeProbleme",
                table: "Incidents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TPEs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumSerie = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Modele = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CommercantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TPEs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TPEs_AspNetUsers_CommercantId",
                        column: x => x.CommercantId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncidentTPEs",
                columns: table => new
                {
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TPEId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateAssociation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentTPEs", x => new { x.IncidentId, x.TPEId });
                    table.ForeignKey(
                        name: "FK_IncidentTPEs_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentTPEs_TPEs_TPEId",
                        column: x => x.TPEId,
                        principalTable: "TPEs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTPEs_TPEId",
                table: "IncidentTPEs",
                column: "TPEId");

            migrationBuilder.CreateIndex(
                name: "IX_TPEs_CommercantId",
                table: "TPEs",
                column: "CommercantId");

            migrationBuilder.CreateIndex(
                name: "IX_TPEs_NumSerie",
                table: "TPEs",
                column: "NumSerie",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PiecesJointes_Incidents_IncidentId",
                table: "PiecesJointes",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PiecesJointes_Incidents_IncidentId",
                table: "PiecesJointes");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_AssigneeId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_CreateurId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "IncidentTPEs");

            migrationBuilder.DropTable(
                name: "TPEs");

            migrationBuilder.DropColumn(
                name: "DateLimite",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Emplacement",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "TypeProbleme",
                table: "Incidents");

            migrationBuilder.RenameColumn(
                name: "IncidentId",
                table: "PiecesJointes",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_PiecesJointes_IncidentId",
                table: "PiecesJointes",
                newName: "IX_PiecesJointes_ApplicationUserId");

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

            migrationBuilder.AddColumn<int>(
                name: "PrioriteTicket",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CheminStockage",
                table: "PiecesJointes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "PiecesJointes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Taille",
                table: "PiecesJointes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "TypePieceJointe",
                table: "PiecesJointes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TitreIncident",
                table: "Incidents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NouveauStatut",
                table: "HistoriquesTicket",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Nom",
                table: "EntitesImpactees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

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
                name: "IX_CommentairesTicket_ApplicationUserId",
                table: "CommentairesTicket",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

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
    }
}
