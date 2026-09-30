using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Requester = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PurchaseLocation = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: true),
                    InstallmentTotal = table.Column<int>(type: "int", nullable: true),
                    CompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    FacilitiesLancamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectCosts_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectCosts_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectCosts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_CompraId",
                table: "ProjectCosts",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_DepartmentId",
                table: "ProjectCosts",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_FacilitiesLancamentoId",
                table: "ProjectCosts",
                column: "FacilitiesLancamentoId",
                unique: true,
                filter: "[FacilitiesLancamentoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_Month_Year",
                table: "ProjectCosts",
                columns: new[] { "Month", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_PaymentMethodId",
                table: "ProjectCosts",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_ProjectId",
                table: "ProjectCosts",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectCosts");
        }
    }
}
