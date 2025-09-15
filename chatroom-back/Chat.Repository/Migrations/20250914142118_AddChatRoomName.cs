using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chat.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddChatRoomName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "chat_rooms",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_chat_rooms_name_MinLength",
                table: "chat_rooms",
                sql: "LENGTH(name) >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_chat_rooms_name_MinLength",
                table: "chat_rooms");

            migrationBuilder.DropColumn(
                name: "name",
                table: "chat_rooms");
        }
    }
}
