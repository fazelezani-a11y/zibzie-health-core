using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zibzie.HealthCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReminderType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Audience = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RelatedRecordType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SensitivityLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientReminders_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientReminders_Audience",
                table: "PatientReminders",
                column: "Audience");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReminders_DueAt",
                table: "PatientReminders",
                column: "DueAt");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReminders_IsDeleted",
                table: "PatientReminders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReminders_PatientProfileId",
                table: "PatientReminders",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReminders_Priority",
                table: "PatientReminders",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReminders_RelatedRecordId",
                table: "PatientReminders",
                column: "RelatedRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReminders_RelatedRecordType",
                table: "PatientReminders",
                column: "RelatedRecordType");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReminders_ReminderType",
                table: "PatientReminders",
                column: "ReminderType");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReminders_Status",
                table: "PatientReminders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientReminders");
        }
    }
}
