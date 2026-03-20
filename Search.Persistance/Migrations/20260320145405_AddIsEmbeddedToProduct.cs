using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Search.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddIsEmbeddedToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmbedded",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmbedded",
                table: "Products");
        }
    }
}
