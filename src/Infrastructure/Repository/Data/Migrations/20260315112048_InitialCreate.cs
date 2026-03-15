using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CKY.MAF.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExecutionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PlanJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SerialGroupCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ParallelGroupCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTasks = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowPartialExecution = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedTasks = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedTasks = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LlmProviderConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModelDisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SupportedScenariosJson = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MaxTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<double>(type: "REAL", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CostPer1kTokens = table.Column<decimal>(type: "TEXT", nullable: false, defaultValue: 0m),
                    AdditionalParametersJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmProviderConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "main_tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_main_tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchedulePlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PlanJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTasks = table.Column<int>(type: "INTEGER", nullable: false),
                    HighPriorityCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumPriorityCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LowPriorityCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulePlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Items = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalTokensUsed = table.Column<long>(type: "INTEGER", nullable: false),
                    TurnCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "TaskExecutionResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PlanId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    DataJson = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    Error = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskExecutionResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sub_tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MainTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sub_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sub_tasks_main_tasks_MainTaskId",
                        column: x => x.MainTaskId,
                        principalTable: "main_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionPlans_CreatedAt",
                table: "ExecutionPlans",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionPlans_PlanId",
                table: "ExecutionPlans",
                column: "PlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionPlans_PlanId_Status",
                table: "ExecutionPlans",
                columns: new[] { "PlanId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionPlans_Status",
                table: "ExecutionPlans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionPlans_Status_CreatedAt",
                table: "ExecutionPlans",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "idx_llm_provider_created_at",
                table: "LlmProviderConfigs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_llm_provider_is_enabled",
                table: "LlmProviderConfigs",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "idx_llm_provider_last_used_at",
                table: "LlmProviderConfigs",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "idx_llm_provider_priority",
                table: "LlmProviderConfigs",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_LlmProviderConfigs_ProviderName",
                table: "LlmProviderConfigs",
                column: "ProviderName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_main_tasks_created_at",
                table: "main_tasks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_main_tasks_priority",
                table: "main_tasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "idx_main_tasks_status",
                table: "main_tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulePlans_CreatedAt",
                table: "SchedulePlans",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulePlans_PlanId",
                table: "SchedulePlans",
                column: "PlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchedulePlans_Status",
                table: "SchedulePlans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulePlans_Status_CreatedAt",
                table: "SchedulePlans",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CreatedAt",
                table: "Sessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ExpiresAt",
                table: "Sessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LastActivityAt",
                table: "Sessions",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_SessionId",
                table: "Sessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Status",
                table: "Sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Status_ExpiresAt",
                table: "Sessions",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId_LastActivityAt",
                table: "Sessions",
                columns: new[] { "UserId", "LastActivityAt" });

            migrationBuilder.CreateIndex(
                name: "idx_sub_tasks_execution_order",
                table: "sub_tasks",
                column: "ExecutionOrder");

            migrationBuilder.CreateIndex(
                name: "idx_sub_tasks_main_task_id",
                table: "sub_tasks",
                column: "MainTaskId");

            migrationBuilder.CreateIndex(
                name: "idx_sub_tasks_status",
                table: "sub_tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionResults_CreatedAt",
                table: "TaskExecutionResults",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionResults_PlanId",
                table: "TaskExecutionResults",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionResults_PlanId_CreatedAt",
                table: "TaskExecutionResults",
                columns: new[] { "PlanId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionResults_PlanId_Success",
                table: "TaskExecutionResults",
                columns: new[] { "PlanId", "Success" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionResults_Success",
                table: "TaskExecutionResults",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionResults_TaskId",
                table: "TaskExecutionResults",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionPlans");

            migrationBuilder.DropTable(
                name: "LlmProviderConfigs");

            migrationBuilder.DropTable(
                name: "SchedulePlans");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "sub_tasks");

            migrationBuilder.DropTable(
                name: "TaskExecutionResults");

            migrationBuilder.DropTable(
                name: "main_tasks");
        }
    }
}
