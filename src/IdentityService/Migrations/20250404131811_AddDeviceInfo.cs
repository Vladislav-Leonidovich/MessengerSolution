using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "UserRefreshTokens",
                type: "longtext",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "UserRefreshTokens",
                type: "longtext",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "UserRefreshTokens",
                type: "longtext",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserRefreshTokens",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLogin",
                table: "UserRefreshTokens",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "UserRefreshTokens",
                type: "longtext",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "OsVersion",
                table: "UserRefreshTokens",
                type: "longtext",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "LastLogin",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "OsVersion",
                table: "UserRefreshTokens");
        }
    }
}
