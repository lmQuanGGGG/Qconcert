using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qconcert.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionPackage123 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PromotionPackages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PromotionPackages");
        }
    }
}
