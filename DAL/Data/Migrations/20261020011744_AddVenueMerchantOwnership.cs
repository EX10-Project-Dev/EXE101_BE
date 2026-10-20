using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueMerchantOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MerchantUserId",
                table: "Venues",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venues_MerchantUserId",
                table: "Venues",
                column: "MerchantUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Venues_AspNetUsers_MerchantUserId",
                table: "Venues",
                column: "MerchantUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Venues_AspNetUsers_MerchantUserId",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Venues_MerchantUserId",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "MerchantUserId",
                table: "Venues");
        }
    }
}
