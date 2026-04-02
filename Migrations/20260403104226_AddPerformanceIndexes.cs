using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuddyScript.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Likes_UserId",
                table: "Likes");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_IsPublic_CreatedAt",
                table: "Posts",
                columns: new[] { "IsPublic", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Likes_EntityId_EntityType",
                table: "Likes",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_Likes_UserId_EntityId_EntityType",
                table: "Likes",
                columns: new[] { "UserId", "EntityId", "EntityType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_IsPublic_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Likes_EntityId_EntityType",
                table: "Likes");

            migrationBuilder.DropIndex(
                name: "IX_Likes_UserId_EntityId_EntityType",
                table: "Likes");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_UserId",
                table: "Likes",
                column: "UserId");
        }
    }
}
