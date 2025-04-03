using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qconcert.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTicketSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TicketType",
                table: "Tickets",
                newName: "ThongTinVe");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "Tickets",
                newName: "ThoiGianKetThucBanVe");

            migrationBuilder.RenameColumn(
                name: "SeatNumber",
                table: "Tickets",
                newName: "TenVe");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "Tickets",
                newName: "ThoiGianBatDauBanVe");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Tickets",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<byte[]>(
                name: "HinhAnhVe",
                table: "Tickets",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "LoaiVe",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SoLuongGhe",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SoVeToiDaTrongMotDonHang",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SoVeToiThieuTrongMotDonHang",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TGBDBanVe",
                table: "Tickets",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "TGKTBanVe",
                table: "Tickets",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HinhAnhVe",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "LoaiVe",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SoLuongGhe",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SoVeToiDaTrongMotDonHang",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SoVeToiThieuTrongMotDonHang",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TGBDBanVe",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TGKTBanVe",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "ThongTinVe",
                table: "Tickets",
                newName: "TicketType");

            migrationBuilder.RenameColumn(
                name: "ThoiGianKetThucBanVe",
                table: "Tickets",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "ThoiGianBatDauBanVe",
                table: "Tickets",
                newName: "EndTime");

            migrationBuilder.RenameColumn(
                name: "TenVe",
                table: "Tickets",
                newName: "SeatNumber");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Tickets",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
