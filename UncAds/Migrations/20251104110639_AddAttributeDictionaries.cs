using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UncAds.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributeDictionaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeDictionaryValue_AttributeDictionary_DictionaryId",
                table: "AttributeDictionaryValue");

            migrationBuilder.DropForeignKey(
                name: "FK_CategoryAttributes_AttributeDictionary_DictionaryId",
                table: "CategoryAttributes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttributeDictionary",
                table: "AttributeDictionary");

            migrationBuilder.RenameTable(
                name: "AttributeDictionary",
                newName: "AttributeDictionaries");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttributeDictionaries",
                table: "AttributeDictionaries",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeDictionaryValue_AttributeDictionaries_DictionaryId",
                table: "AttributeDictionaryValue",
                column: "DictionaryId",
                principalTable: "AttributeDictionaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryAttributes_AttributeDictionaries_DictionaryId",
                table: "CategoryAttributes",
                column: "DictionaryId",
                principalTable: "AttributeDictionaries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeDictionaryValue_AttributeDictionaries_DictionaryId",
                table: "AttributeDictionaryValue");

            migrationBuilder.DropForeignKey(
                name: "FK_CategoryAttributes_AttributeDictionaries_DictionaryId",
                table: "CategoryAttributes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttributeDictionaries",
                table: "AttributeDictionaries");

            migrationBuilder.RenameTable(
                name: "AttributeDictionaries",
                newName: "AttributeDictionary");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttributeDictionary",
                table: "AttributeDictionary",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeDictionaryValue_AttributeDictionary_DictionaryId",
                table: "AttributeDictionaryValue",
                column: "DictionaryId",
                principalTable: "AttributeDictionary",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryAttributes_AttributeDictionary_DictionaryId",
                table: "CategoryAttributes",
                column: "DictionaryId",
                principalTable: "AttributeDictionary",
                principalColumn: "Id");
        }
    }
}
