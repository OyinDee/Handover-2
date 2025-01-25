using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Handover_2.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTaskConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistory_Notifications_NotificationId",
                table: "NotificationHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistory_Tasks_TaskId",
                table: "NotificationHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_AspNetUsers_AssignedToUserId",
                table: "Tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotificationHistory",
                table: "NotificationHistory");

            migrationBuilder.RenameTable(
                name: "NotificationHistory",
                newName: "NotificationHistories");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationHistory_TaskId",
                table: "NotificationHistories",
                newName: "IX_NotificationHistories_TaskId");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationHistory_NotificationId",
                table: "NotificationHistories",
                newName: "IX_NotificationHistories_NotificationId");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Tasks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Tasks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "Tasks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Tasks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotificationHistories",
                table: "NotificationHistories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistories_Notifications_NotificationId",
                table: "NotificationHistories",
                column: "NotificationId",
                principalTable: "Notifications",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistories_Tasks_TaskId",
                table: "NotificationHistories",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_AspNetUsers_AssignedToUserId",
                table: "Tasks",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistories_Notifications_NotificationId",
                table: "NotificationHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistories_Tasks_TaskId",
                table: "NotificationHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_AspNetUsers_AssignedToUserId",
                table: "Tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotificationHistories",
                table: "NotificationHistories");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Tasks");

            migrationBuilder.RenameTable(
                name: "NotificationHistories",
                newName: "NotificationHistory");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationHistories_TaskId",
                table: "NotificationHistory",
                newName: "IX_NotificationHistory_TaskId");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationHistories_NotificationId",
                table: "NotificationHistory",
                newName: "IX_NotificationHistory_NotificationId");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Tasks",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "Tasks",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotificationHistory",
                table: "NotificationHistory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistory_Notifications_NotificationId",
                table: "NotificationHistory",
                column: "NotificationId",
                principalTable: "Notifications",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistory_Tasks_TaskId",
                table: "NotificationHistory",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_AspNetUsers_AssignedToUserId",
                table: "Tasks",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
