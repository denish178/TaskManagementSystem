using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.File.Migrations
{
    /// <inheritdoc />
    public partial class AlignAttachmentIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM [Attachments];
                """);

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "UploadedBy",
                table: "Attachments");

            migrationBuilder.AddColumn<Guid>(
                name: "TaskId",
                table: "Attachments",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UploadedBy",
                table: "Attachments",
                type: "uniqueidentifier",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM [Attachments];
                """);

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "UploadedBy",
                table: "Attachments");

            migrationBuilder.AddColumn<int>(
                name: "TaskId",
                table: "Attachments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UploadedBy",
                table: "Attachments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
