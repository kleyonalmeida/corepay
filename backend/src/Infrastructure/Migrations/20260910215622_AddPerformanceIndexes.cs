using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrafficProjectDeposits_ProjectId",
                table: "TrafficProjectDeposits");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficProjectDeposits_ProjectId_DepositDate",
                table: "TrafficProjectDeposits",
                columns: new[] { "ProjectId", "DepositDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_Month_Year_Type",
                table: "ProjectCosts",
                columns: new[] { "Month", "Year", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_Year_Month_DepartmentId",
                table: "Payrolls",
                columns: new[] { "Year", "Month", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_Year_Status",
                table: "Payrolls",
                columns: new[] { "Year", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Collaborators_DepartmentId_IsActive",
                table: "Collaborators",
                columns: new[] { "DepartmentId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrafficProjectDeposits_ProjectId_DepositDate",
                table: "TrafficProjectDeposits");

            migrationBuilder.DropIndex(
                name: "IX_ProjectCosts_Month_Year_Type",
                table: "ProjectCosts");

            migrationBuilder.DropIndex(
                name: "IX_Payrolls_Year_Month_DepartmentId",
                table: "Payrolls");

            migrationBuilder.DropIndex(
                name: "IX_Payrolls_Year_Status",
                table: "Payrolls");

            migrationBuilder.DropIndex(
                name: "IX_Collaborators_DepartmentId_IsActive",
                table: "Collaborators");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficProjectDeposits_ProjectId",
                table: "TrafficProjectDeposits",
                column: "ProjectId");
        }
    }
}
