using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MZikmund.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CommentDisplayNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasChosenDisplayName",
                table: "CommentAuthor",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasChosenDisplayName",
                table: "CommentAuthor");
        }
    }
}
