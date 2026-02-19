using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projet0.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntitesImpactees_Incidents_IncidentId",
                table: "EntitesImpactees");

            migrationBuilder.AlterColumn<Guid>(
                name: "IncidentId",
                table: "EntitesImpactees",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EntitesImpactees_Incidents_IncidentId",
                table: "EntitesImpactees",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntitesImpactees_Incidents_IncidentId",
                table: "EntitesImpactees");

            migrationBuilder.AlterColumn<Guid>(
                name: "IncidentId",
                table: "EntitesImpactees",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_EntitesImpactees_Incidents_IncidentId",
                table: "EntitesImpactees",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
