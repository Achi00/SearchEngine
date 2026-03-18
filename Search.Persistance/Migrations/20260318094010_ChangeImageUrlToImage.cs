using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Search.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class ChangeImageUrlToImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Products",
                newName: "Image");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Image",
                table: "Products",
                newName: "ImageUrl");
        }
    }
}
