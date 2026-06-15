using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zibzie.HealthCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientTimelineEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RelatedRecordType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SensitivityLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientTimelineEvents_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientTimelineEvents_EventType",
                table: "PatientTimelineEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_PatientTimelineEvents_IsDeleted",
                table: "PatientTimelineEvents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PatientTimelineEvents_OccurredAt",
                table: "PatientTimelineEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_PatientTimelineEvents_PatientProfileId",
                table: "PatientTimelineEvents",
                column: "PatientProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientTimelineEvents");
        }
    }
}
