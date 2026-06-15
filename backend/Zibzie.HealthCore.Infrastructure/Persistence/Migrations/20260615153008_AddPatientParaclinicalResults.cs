using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zibzie.HealthCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientParaclinicalResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientParaclinicalResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResultType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PerformedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResultDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProviderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkedDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Interpretation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsAbnormal = table.Column<bool>(type: "boolean", nullable: true),
                    RequiresFollowUp = table.Column<bool>(type: "boolean", nullable: false),
                    FollowUpNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_PatientParaclinicalResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientParaclinicalResults_PatientDocuments_LinkedDocumentId",
                        column: x => x.LinkedDocumentId,
                        principalTable: "PatientDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientParaclinicalResults_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientLabResultItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientParaclinicalResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NumericValue = table.Column<decimal>(type: "numeric", nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenceRange = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsAbnormal = table.Column<bool>(type: "boolean", nullable: true),
                    Interpretation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientLabResultItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientLabResultItems_PatientParaclinicalResults_PatientPar~",
                        column: x => x.PatientParaclinicalResultId,
                        principalTable: "PatientParaclinicalResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientLabResultItems_DisplayOrder",
                table: "PatientLabResultItems",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_PatientLabResultItems_PatientParaclinicalResultId",
                table: "PatientLabResultItems",
                column: "PatientParaclinicalResultId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientParaclinicalResults_IsDeleted",
                table: "PatientParaclinicalResults",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PatientParaclinicalResults_LinkedDocumentId",
                table: "PatientParaclinicalResults",
                column: "LinkedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientParaclinicalResults_PatientProfileId",
                table: "PatientParaclinicalResults",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientParaclinicalResults_PerformedAt",
                table: "PatientParaclinicalResults",
                column: "PerformedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PatientParaclinicalResults_RequiresFollowUp",
                table: "PatientParaclinicalResults",
                column: "RequiresFollowUp");

            migrationBuilder.CreateIndex(
                name: "IX_PatientParaclinicalResults_ResultDate",
                table: "PatientParaclinicalResults",
                column: "ResultDate");

            migrationBuilder.CreateIndex(
                name: "IX_PatientParaclinicalResults_ResultType",
                table: "PatientParaclinicalResults",
                column: "ResultType");

            migrationBuilder.CreateIndex(
                name: "IX_PatientParaclinicalResults_SensitivityLevel",
                table: "PatientParaclinicalResults",
                column: "SensitivityLevel");

            migrationBuilder.CreateIndex(
                name: "IX_PatientParaclinicalResults_VerificationStatus",
                table: "PatientParaclinicalResults",
                column: "VerificationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientLabResultItems");

            migrationBuilder.DropTable(
                name: "PatientParaclinicalResults");
        }
    }
}
