using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zibzie.HealthCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceAccountId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductRole = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Permission = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    AccessScope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AuthorizationReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequestPath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    HttpMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ActionType",
                table: "AuditLogEntries",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_CorrelationId",
                table: "AuditLogEntries",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_CreatedAt",
                table: "AuditLogEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_PatientId",
                table: "AuditLogEntries",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_PatientId_CreatedAt",
                table: "AuditLogEntries",
                columns: new[] { "PatientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ProductCode",
                table: "AuditLogEntries",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ProductCode_CreatedAt",
                table: "AuditLogEntries",
                columns: new[] { "ProductCode", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ResourceType",
                table: "AuditLogEntries",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ServiceAccountId",
                table: "AuditLogEntries",
                column: "ServiceAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_Succeeded",
                table: "AuditLogEntries",
                column: "Succeeded");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_UserId",
                table: "AuditLogEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_UserId_CreatedAt",
                table: "AuditLogEntries",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogEntries");
        }
    }
}
