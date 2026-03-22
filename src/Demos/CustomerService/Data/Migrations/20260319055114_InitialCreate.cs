using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CKY.MultiAgentFramework.Demos.CustomerService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cs_customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    PreferredLanguage = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, defaultValue: "zh-CN"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cs_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cs_faq_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Question = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Answer = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    KeywordsJson = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cs_faq_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cs_user_behavior_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Intent = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TaskSucceeded = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClarificationRoundsNeeded = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponseTimeMs = table.Column<long>(type: "INTEGER", nullable: false),
                    EntitiesJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cs_user_behavior_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cs_chat_sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cs_chat_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cs_chat_sessions_cs_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "cs_customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cs_orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TrackingNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cs_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cs_orders_cs_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "cs_customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cs_tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TicketId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RelatedOrderId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cs_tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cs_tickets_cs_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "cs_customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cs_chat_messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChatSessionEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Intent = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    EntitiesJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cs_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cs_chat_messages_cs_chat_sessions_ChatSessionEntityId",
                        column: x => x.ChatSessionEntityId,
                        principalTable: "cs_chat_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cs_order_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cs_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cs_order_items_cs_orders_OrderEntityId",
                        column: x => x.OrderEntityId,
                        principalTable: "cs_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cs_tracking_events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrackingNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cs_tracking_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cs_tracking_events_cs_orders_OrderEntityId",
                        column: x => x.OrderEntityId,
                        principalTable: "cs_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cs_ticket_comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TicketEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    IsStaff = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cs_ticket_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cs_ticket_comments_cs_tickets_TicketEntityId",
                        column: x => x.TicketEntityId,
                        principalTable: "cs_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_cs_chat_messages_session_id",
                table: "cs_chat_messages",
                column: "ChatSessionEntityId");

            migrationBuilder.CreateIndex(
                name: "idx_cs_chat_messages_timestamp",
                table: "cs_chat_messages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_cs_chat_sessions_customer_id",
                table: "cs_chat_sessions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "idx_cs_chat_sessions_session_id",
                table: "cs_chat_sessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cs_chat_sessions_started_at",
                table: "cs_chat_sessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "idx_cs_chat_sessions_status",
                table: "cs_chat_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "idx_cs_customers_customer_id",
                table: "cs_customers",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cs_customers_email",
                table: "cs_customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "idx_cs_faq_entries_category",
                table: "cs_faq_entries",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "idx_cs_faq_entries_is_active",
                table: "cs_faq_entries",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_cs_order_items_OrderEntityId",
                table: "cs_order_items",
                column: "OrderEntityId");

            migrationBuilder.CreateIndex(
                name: "idx_cs_orders_created_at",
                table: "cs_orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_cs_orders_customer_id",
                table: "cs_orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "idx_cs_orders_order_id",
                table: "cs_orders",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cs_orders_status",
                table: "cs_orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cs_ticket_comments_TicketEntityId",
                table: "cs_ticket_comments",
                column: "TicketEntityId");

            migrationBuilder.CreateIndex(
                name: "idx_cs_tickets_created_at",
                table: "cs_tickets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_cs_tickets_customer_id",
                table: "cs_tickets",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "idx_cs_tickets_priority",
                table: "cs_tickets",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "idx_cs_tickets_status",
                table: "cs_tickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "idx_cs_tickets_ticket_id",
                table: "cs_tickets",
                column: "TicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cs_tracking_events_order_id",
                table: "cs_tracking_events",
                column: "OrderEntityId");

            migrationBuilder.CreateIndex(
                name: "idx_cs_tracking_events_tracking_number",
                table: "cs_tracking_events",
                column: "TrackingNumber");

            migrationBuilder.CreateIndex(
                name: "idx_cs_user_behavior_intent",
                table: "cs_user_behavior_records",
                column: "Intent");

            migrationBuilder.CreateIndex(
                name: "idx_cs_user_behavior_timestamp",
                table: "cs_user_behavior_records",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_cs_user_behavior_user_id",
                table: "cs_user_behavior_records",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cs_chat_messages");

            migrationBuilder.DropTable(
                name: "cs_faq_entries");

            migrationBuilder.DropTable(
                name: "cs_order_items");

            migrationBuilder.DropTable(
                name: "cs_ticket_comments");

            migrationBuilder.DropTable(
                name: "cs_tracking_events");

            migrationBuilder.DropTable(
                name: "cs_user_behavior_records");

            migrationBuilder.DropTable(
                name: "cs_chat_sessions");

            migrationBuilder.DropTable(
                name: "cs_tickets");

            migrationBuilder.DropTable(
                name: "cs_orders");

            migrationBuilder.DropTable(
                name: "cs_customers");
        }
    }
}
