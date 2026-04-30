using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projet0.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <inheritdoc />
    public partial class RemoveOldArchiveFieldsFromIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Supprimer les colonnes obsolètes de la table Incidents
            migrationBuilder.DropColumn(
                name: "EstArchive",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "DateArchivage",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ArchiveParId",
                table: "Incidents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recréer les colonnes en cas de rollback
            migrationBuilder.AddColumn<bool>(
                name: "EstArchive",
                table: "Incidents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateArchivage",
                table: "Incidents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ArchiveParId",
                table: "Incidents",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
