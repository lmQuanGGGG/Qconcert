using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qconcert.Migrations
{
    /// <inheritdoc />
    public partial class AddSoLuongConLaiToTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SoLuongConLai",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoLuongConLai",
                table: "Tickets");
        }
    }
}
