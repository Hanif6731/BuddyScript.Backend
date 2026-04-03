using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuddyScript.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddReactionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReactionType",
                table: "Likes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReactionType",
                table: "Likes");
        }
    }
}
