using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sushi.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImagePath",
                table: "Products",
                newName: "ImageFileName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageFileName",
                table: "Products",
                newName: "ImagePath");
        }
    }
}
