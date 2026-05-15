using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWatch.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CheckId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    FiredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RepeatCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", nullable: false),
                    LanViewerEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LanViewerPort = table.Column<int>(type: "INTEGER", nullable: false),
                    LanSharedSecretHash = table.Column<string>(type: "TEXT", nullable: true),
                    LastSeenReleaseVersion = table.Column<string>(type: "TEXT", nullable: true),
                    PortableDataPath = table.Column<bool>(type: "INTEGER", nullable: false),
                    PortableDataDirectory = table.Column<string>(type: "TEXT", nullable: true),
                    StartWithWindows = table.Column<bool>(type: "INTEGER", nullable: false),
                    MonitoringPaused = table.Column<bool>(type: "INTEGER", nullable: false),
                    AlertsMutedUntilRestart = table.Column<bool>(type: "INTEGER", nullable: false),
                    AlertsMutedUntilUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DefaultWebhookUrl = table.Column<string>(type: "TEXT", nullable: true),
                    OnboardingCompleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Targets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Host = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MuteUntilUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Targets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Checks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ParametersJson = table.Column<string>(type: "TEXT", nullable: true),
                    IntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    WarnThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    DownThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    NextRunUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsecutiveFailures = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsecutiveSuccesses = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Checks_Targets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CheckId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ToastEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SoundEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    WebhookEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    WebhookUrl = table.Column<string>(type: "TEXT", nullable: true),
                    RepeatMinutes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertRules_Checks_CheckId",
                        column: x => x.CheckId,
                        principalTable: "Checks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CheckId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    LatencyMs = table.Column<double>(type: "REAL", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Results_Checks_CheckId",
                        column: x => x.CheckId,
                        principalTable: "Checks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Id", "AlertsMutedUntilRestart", "AlertsMutedUntilUtc", "DefaultWebhookUrl", "LanSharedSecretHash", "LanViewerEnabled", "LanViewerPort", "LastSeenReleaseVersion", "MonitoringPaused", "OnboardingCompleted", "PortableDataDirectory", "PortableDataPath", "RetentionDays", "StartWithWindows", "Theme" },
                values: new object[] { 1, false, null, null, null, false, 5080, null, false, false, null, false, 30, false, "Dark" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_FiredAtUtc",
                table: "AlertEvents",
                column: "FiredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_CheckId",
                table: "AlertRules",
                column: "CheckId");

            migrationBuilder.CreateIndex(
                name: "IX_Checks_NextRunUtc",
                table: "Checks",
                column: "NextRunUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Checks_TargetId",
                table: "Checks",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_CheckId_TimestampUtc",
                table: "Results",
                columns: new[] { "CheckId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Results_TimestampUtc",
                table: "Results",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Targets_Tag",
                table: "Targets",
                column: "Tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertEvents");

            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Checks");

            migrationBuilder.DropTable(
                name: "Targets");
        }
    }
}
