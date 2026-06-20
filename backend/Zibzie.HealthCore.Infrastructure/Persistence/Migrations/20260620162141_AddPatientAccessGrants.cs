using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zibzie.HealthCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientAccessGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientAccessGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceAccountId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductRole = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AccessScope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AuthorizationReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GrantNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokeReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAccessGrants_PatientProfiles_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessGrants_PatientId",
                table: "PatientAccessGrants",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessGrants_PatientId_ProductCode",
                table: "PatientAccessGrants",
                columns: new[] { "PatientId", "ProductCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessGrants_ProductCode",
                table: "PatientAccessGrants",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessGrants_ProductCode_ProductRole",
                table: "PatientAccessGrants",
                columns: new[] { "ProductCode", "ProductRole" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessGrants_RevokedAt",
                table: "PatientAccessGrants",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessGrants_ServiceAccountId",
                table: "PatientAccessGrants",
                column: "ServiceAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessGrants_UserId",
                table: "PatientAccessGrants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessGrants_UserId_PatientId_ProductCode",
                table: "PatientAccessGrants",
                columns: new[] { "UserId", "PatientId", "ProductCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessGrants_ValidUntil",
                table: "PatientAccessGrants",
                column: "ValidUntil");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientAccessGrants");
        }
    }
}
