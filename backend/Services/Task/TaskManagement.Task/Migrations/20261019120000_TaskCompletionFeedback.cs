using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Task.Migrations;

public partial class TaskCompletionFeedback : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "CompletedByUserId",
            table: "Tasks",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CompletionFeedback",
            table: "Tasks",
            type: "nvarchar(4000)",
            maxLength: 4000,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CompletionFeedbackSubmittedAt",
            table: "Tasks",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CompletedByUserId", table: "Tasks");
        migrationBuilder.DropColumn(name: "CompletionFeedback", table: "Tasks");
        migrationBuilder.DropColumn(name: "CompletionFeedbackSubmittedAt", table: "Tasks");
    }
}
