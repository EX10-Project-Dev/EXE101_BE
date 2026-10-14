using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueSourceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "Venues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venues_SourceId",
                table: "Venues",
                column: "SourceId",
                unique: true,
                filter: "[SourceId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venues_SourceId",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Venues");
        }
    }
}
