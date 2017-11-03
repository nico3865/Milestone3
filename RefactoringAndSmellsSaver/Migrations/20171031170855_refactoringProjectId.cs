using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RefactoringAndSmellsSaver.Migrations
{
    public partial class refactoringProjectId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ProjectId",
                table: "Refactorings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Refactorings_ProjectId",
                table: "Refactorings",
                column: "ProjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Refactorings_ProjectId",
                table: "Refactorings");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Refactorings");
        }
    }
}
