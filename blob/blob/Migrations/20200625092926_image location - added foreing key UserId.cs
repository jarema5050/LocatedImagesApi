using Microsoft.EntityFrameworkCore.Migrations;

namespace blob.Migrations
{
    public partial class imagelocationaddedforeingkeyUserId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "ImageLocation",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_ImageLocation_UserId",
                table: "ImageLocation",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImageLocation_User_UserId",
                table: "ImageLocation",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageLocation_User_UserId",
                table: "ImageLocation");

            migrationBuilder.DropIndex(
                name: "IX_ImageLocation_UserId",
                table: "ImageLocation");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ImageLocation");
        }
    }
}
