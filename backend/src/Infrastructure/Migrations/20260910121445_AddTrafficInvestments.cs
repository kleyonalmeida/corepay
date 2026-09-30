using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrafficInvestments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrafficInvestments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    MonthlyTarget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficInvestments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrafficInvestments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrafficProjectDeposits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepositDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficProjectDeposits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrafficProjectDeposits_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrafficInvestmentWeeks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrafficInvestmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficInvestmentWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrafficInvestmentWeeks_TrafficInvestments_TrafficInvestmentId",
                        column: x => x.TrafficInvestmentId,
                        principalTable: "TrafficInvestments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrafficWeekChannelSpends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrafficInvestmentWeekId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficWeekChannelSpends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrafficWeekChannelSpends_TrafficInvestmentWeeks_TrafficInvestmentWeekId",
                        column: x => x.TrafficInvestmentWeekId,
                        principalTable: "TrafficInvestmentWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrafficWeekDeposits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrafficInvestmentWeekId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficWeekDeposits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrafficWeekDeposits_TrafficInvestmentWeeks_TrafficInvestmentWeekId",
                        column: x => x.TrafficInvestmentWeekId,
                        principalTable: "TrafficInvestmentWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrafficInvestments_ProjectId_Month_Year",
                table: "TrafficInvestments",
                columns: new[] { "ProjectId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrafficInvestmentWeeks_TrafficInvestmentId_WeekNumber",
                table: "TrafficInvestmentWeeks",
                columns: new[] { "TrafficInvestmentId", "WeekNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrafficProjectDeposits_ProjectId",
                table: "TrafficProjectDeposits",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficWeekChannelSpends_TrafficInvestmentWeekId_Channel",
                table: "TrafficWeekChannelSpends",
                columns: new[] { "TrafficInvestmentWeekId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrafficWeekDeposits_TrafficInvestmentWeekId",
                table: "TrafficWeekDeposits",
                column: "TrafficInvestmentWeekId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrafficProjectDeposits");

            migrationBuilder.DropTable(
                name: "TrafficWeekChannelSpends");

            migrationBuilder.DropTable(
                name: "TrafficWeekDeposits");

            migrationBuilder.DropTable(
                name: "TrafficInvestmentWeeks");

            migrationBuilder.DropTable(
                name: "TrafficInvestments");
        }
    }
}
