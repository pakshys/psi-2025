using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMembersToPartyRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_UserId",
                schema: "app",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Friendships_AddresseeId",
                schema: "app",
                table: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_Friendships_RequesterId",
                schema: "app",
                table: "Friendships");

            migrationBuilder.AddColumn<List<string>>(
                name: "Members",
                schema: "app",
                table: "PartyRooms",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                schema: "app",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_AddresseeId_RequesterId",
                schema: "app",
                table: "Friendships",
                columns: new[] { "AddresseeId", "RequesterId" });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_RequesterId_AddresseeId",
                schema: "app",
                table: "Friendships",
                columns: new[] { "RequesterId", "AddresseeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_UserId",
                schema: "app",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Friendships_AddresseeId_RequesterId",
                schema: "app",
                table: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_Friendships_RequesterId_AddresseeId",
                schema: "app",
                table: "Friendships");

            migrationBuilder.DropColumn(
                name: "Members",
                schema: "app",
                table: "PartyRooms");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                schema: "app",
                table: "UserProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_AddresseeId",
                schema: "app",
                table: "Friendships",
                column: "AddresseeId");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_RequesterId",
                schema: "app",
                table: "Friendships",
                column: "RequesterId");
        }
    }
}
