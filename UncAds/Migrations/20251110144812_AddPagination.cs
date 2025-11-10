using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UncAds.Migrations
{
    /// <inheritdoc />
    public partial class AddPagination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdsPerPage",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdsPerPage",
                table: "AspNetUsers");
        }
    }
}
