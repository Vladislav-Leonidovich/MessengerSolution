using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageService.Migrations.MessageDeliverySagaDb
{
    /// <inheritdoc />
    public partial class InitialSagaCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DeleteAllMessagesSagas",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CurrentState = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    ChatRoomId = table.Column<int>(type: "int", nullable: false),
                    InitiatedByUserId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedMessageCount = table.Column<int>(type: "int", nullable: false),
                    NotifiedUserIds = table.Column<string>(type: "longtext", nullable: false),
                    TimeoutTokenId = table.Column<Guid>(type: "char(36)", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastError = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeleteAllMessagesSagas", x => x.CorrelationId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MessageDeliverySagas",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CurrentState = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    ChatRoomId = table.Column<int>(type: "int", nullable: false),
                    SenderUserId = table.Column<int>(type: "int", nullable: false),
                    EncryptedContent = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsSaved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDelivered = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeliveredAfterTimeout = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeliveredToUserIds = table.Column<string>(type: "longtext", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    DeliveryTimeoutTokenId = table.Column<Guid>(type: "char(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageDeliverySagas", x => x.CorrelationId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeleteAllMessagesSagas");

            migrationBuilder.DropTable(
                name: "MessageDeliverySagas");
        }
    }
}
