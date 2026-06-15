using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zibzie.HealthCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCarePlanItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarePlanItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequestedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AssignedTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PlannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResultSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NextAction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RelatedRecordType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SensitivityLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarePlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarePlanItems_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarePlanItems_Category",
                table: "CarePlanItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlanItems_DueAt",
                table: "CarePlanItems",
                column: "DueAt");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlanItems_IsDeleted",
                table: "CarePlanItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlanItems_ItemType",
                table: "CarePlanItems",
                column: "ItemType");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlanItems_PatientProfileId",
                table: "CarePlanItems",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlanItems_PlannedAt",
                table: "CarePlanItems",
                column: "PlannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlanItems_Priority",
                table: "CarePlanItems",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlanItems_RelatedRecordId",
                table: "CarePlanItems",
                column: "RelatedRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlanItems_RelatedRecordType",
                table: "CarePlanItems",
                column: "RelatedRecordType");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlanItems_Status",
                table: "CarePlanItems",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarePlanItems");
        }
    }
}
