using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RefactoringAndSmellsSaver.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganicMetrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CyclomaticComplexity = table.Column<double>(type: "REAL", nullable: true),
                    LocalityRatio = table.Column<double>(type: "REAL", nullable: true),
                    MaxCallChain = table.Column<double>(type: "REAL", nullable: true),
                    MethodLinesOfCode = table.Column<double>(type: "REAL", nullable: true),
                    ParameterCount = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganicMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Refactorings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommitId = table.Column<string>(type: "TEXT", nullable: true),
                    SourceAttributeName = table.Column<string>(type: "TEXT", nullable: true),
                    SourceClassName = table.Column<string>(type: "TEXT", nullable: true),
                    SourceClassPackageName = table.Column<string>(type: "TEXT", nullable: true),
                    SourceClassPath = table.Column<string>(type: "TEXT", nullable: true),
                    SourceOperatationName = table.Column<string>(type: "TEXT", nullable: true),
                    TargetClassName = table.Column<string>(type: "TEXT", nullable: true),
                    TargetClassPackageName = table.Column<string>(type: "TEXT", nullable: true),
                    TargetClassPath = table.Column<string>(type: "TEXT", nullable: true),
                    TargetOperatationName = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refactorings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Commits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AuthorName = table.Column<string>(type: "TEXT", nullable: true),
                    BranchName = table.Column<string>(type: "TEXT", nullable: true),
                    CommitId = table.Column<string>(type: "TEXT", nullable: true),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FullMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<long>(type: "INTEGER", nullable: false),
                    ShortMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commits_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganicClasses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommitId = table.Column<long>(type: "INTEGER", nullable: false),
                    FullyQualifiedName = table.Column<string>(type: "TEXT", nullable: true),
                    MetricsValuesId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganicClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganicClasses_Commits_CommitId",
                        column: x => x.CommitId,
                        principalTable: "Commits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganicClasses_OrganicMetrics_MetricsValuesId",
                        column: x => x.MetricsValuesId,
                        principalTable: "OrganicMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganicMethods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullyQualifiedName = table.Column<string>(type: "TEXT", nullable: true),
                    MetricsValuesId = table.Column<long>(type: "INTEGER", nullable: true),
                    OrganicClassId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganicMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganicMethods_OrganicMetrics_MetricsValuesId",
                        column: x => x.MetricsValuesId,
                        principalTable: "OrganicMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganicMethods_OrganicClasses_OrganicClassId",
                        column: x => x.OrganicClassId,
                        principalTable: "OrganicClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganicSmell",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    OrganicClassId = table.Column<long>(type: "INTEGER", nullable: true),
                    OrganicMethodId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganicSmell", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganicSmell_OrganicClasses_OrganicClassId",
                        column: x => x.OrganicClassId,
                        principalTable: "OrganicClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganicSmell_OrganicMethods_OrganicMethodId",
                        column: x => x.OrganicMethodId,
                        principalTable: "OrganicMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commits_AuthorName",
                table: "Commits",
                column: "AuthorName");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_CommitId",
                table: "Commits",
                column: "CommitId");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_ProjectId",
                table: "Commits",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicClasses_CommitId",
                table: "OrganicClasses",
                column: "CommitId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicClasses_FullyQualifiedName",
                table: "OrganicClasses",
                column: "FullyQualifiedName");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicClasses_MetricsValuesId",
                table: "OrganicClasses",
                column: "MetricsValuesId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicMethods_FullyQualifiedName",
                table: "OrganicMethods",
                column: "FullyQualifiedName");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicMethods_MetricsValuesId",
                table: "OrganicMethods",
                column: "MetricsValuesId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicMethods_OrganicClassId",
                table: "OrganicMethods",
                column: "OrganicClassId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicSmell_Name",
                table: "OrganicSmell",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicSmell_OrganicClassId",
                table: "OrganicSmell",
                column: "OrganicClassId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicSmell_OrganicMethodId",
                table: "OrganicSmell",
                column: "OrganicMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Refactorings_CommitId",
                table: "Refactorings",
                column: "CommitId");

            migrationBuilder.CreateIndex(
                name: "IX_Refactorings_Type",
                table: "Refactorings",
                column: "Type");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganicSmell");

            migrationBuilder.DropTable(
                name: "Refactorings");

            migrationBuilder.DropTable(
                name: "OrganicMethods");

            migrationBuilder.DropTable(
                name: "OrganicClasses");

            migrationBuilder.DropTable(
                name: "Commits");

            migrationBuilder.DropTable(
                name: "OrganicMetrics");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
