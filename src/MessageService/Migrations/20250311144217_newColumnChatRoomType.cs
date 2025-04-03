using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageService.Migrations
{
    /// <inheritdoc />
    public partial class newColumnChatRoomType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChatRoomType",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatRoomType",
                table: "Messages");
        }
    }
}
