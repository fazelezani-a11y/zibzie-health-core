using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zibzie.HealthCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeasurementType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BodySite = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Context = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReferenceRange = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsAbnormal = table.Column<bool>(type: "boolean", nullable: true),
                    TargetMin = table.Column<decimal>(type: "numeric", nullable: true),
                    TargetMax = table.Column<decimal>(type: "numeric", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RelatedRecordType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    VerificationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SensitivityLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientMeasurements_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMeasurements_IsDeleted",
                table: "PatientMeasurements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMeasurements_MeasuredAt",
                table: "PatientMeasurements",
                column: "MeasuredAt");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMeasurements_MeasurementType",
                table: "PatientMeasurements",
                column: "MeasurementType");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMeasurements_PatientProfileId",
                table: "PatientMeasurements",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMeasurements_RelatedRecordId",
                table: "PatientMeasurements",
                column: "RelatedRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMeasurements_RelatedRecordType",
                table: "PatientMeasurements",
                column: "RelatedRecordType");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMeasurements_SensitivityLevel",
                table: "PatientMeasurements",
                column: "SensitivityLevel");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMeasurements_SourceType",
                table: "PatientMeasurements",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMeasurements_VerificationStatus",
                table: "PatientMeasurements",
                column: "VerificationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientMeasurements");
        }
    }
}
