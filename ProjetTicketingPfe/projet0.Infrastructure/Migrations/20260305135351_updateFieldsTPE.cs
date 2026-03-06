using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projet0.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateFieldsTPE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TPEs_NumSerie",
                table: "TPEs");

            migrationBuilder.AlterColumn<int>(
                name: "Modele",
                table: "TPEs",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "NumSerieComplet",
                table: "TPEs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TPEs_NumSerie_Modele",
                table: "TPEs",
                columns: new[] { "NumSerie", "Modele" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TPEs_NumSerie_Modele",
                table: "TPEs");

            migrationBuilder.DropColumn(
                name: "NumSerieComplet",
                table: "TPEs");

            migrationBuilder.AlterColumn<string>(
                name: "Modele",
                table: "TPEs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_TPEs_NumSerie",
                table: "TPEs",
                column: "NumSerie",
                unique: true);
        }
    }
}
