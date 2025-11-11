using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UncAds.Migrations
{
    /// <inheritdoc />
    public partial class AddHomePageMessageToAdminSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HomePageMessage",
                table: "AdminSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HomePageMessage",
                table: "AdminSettings");
        }
    }
}
