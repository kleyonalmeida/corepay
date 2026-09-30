using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payrolls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RejectionComment = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payrolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payrolls_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollCollaboratorEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollaboratorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollaboratorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PixKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AdmissionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CareerLevelName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CalculationProfile = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CareerLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FullBaseSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GoalTier = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FinalSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BetanoInternaCount = table.Column<int>(type: "int", nullable: false),
                    BetanoMundoBetCount = table.Column<int>(type: "int", nullable: false),
                    SupervisorAnalystRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionPayingProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TrafficSeniorLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    NfSent = table.Column<bool>(type: "bit", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCollaboratorEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollCollaboratorEntries_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCollaboratorEntries_PayrollId_CollaboratorId",
                table: "PayrollCollaboratorEntries",
                columns: new[] { "PayrollId", "CollaboratorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_DepartmentId_Month_Year",
                table: "Payrolls",
                columns: new[] { "DepartmentId", "Month", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollCollaboratorEntries");

            migrationBuilder.DropTable(
                name: "Payrolls");
        }
    }
}
