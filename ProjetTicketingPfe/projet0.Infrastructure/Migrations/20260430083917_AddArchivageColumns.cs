using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projet0.Infrastructure.Migrations
{
    public partial class AddArchivageColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Vérifier si les colonnes existent déjà avant de les ajouter
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Incidents' AND COLUMN_NAME = 'EstArchive')
                BEGIN
                    ALTER TABLE Incidents ADD EstArchive BIT NOT NULL DEFAULT 0;
                    ALTER TABLE Incidents ADD DateArchivage DATETIME2 NULL;
                    ALTER TABLE Incidents ADD ArchiveParId UNIQUEIDENTIFIER NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE Incidents DROP COLUMN IF EXISTS EstArchive;
                ALTER TABLE Incidents DROP COLUMN IF EXISTS DateArchivage;
                ALTER TABLE Incidents DROP COLUMN IF EXISTS ArchiveParId;
            ");
        }
    }
}