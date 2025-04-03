using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qconcert.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAllAspNetRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM AspNetRoles");
            migrationBuilder.Sql("DELETE FROM AspNetUsers");
            migrationBuilder.Sql("DELETE FROM AspNetUserRoles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
