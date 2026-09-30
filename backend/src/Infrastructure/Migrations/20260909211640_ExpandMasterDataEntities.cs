using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandMasterDataEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ClientName",
                table: "Projects",
                newName: "Client");

            migrationBuilder.RenameColumn(
                name: "CommissionPercentage",
                table: "CareerLevels",
                newName: "TrafficSupCommissionPct");

            migrationBuilder.AddColumn<decimal>(
                name: "BetanoInternaValue",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BetanoMundoBetValue",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionWithGoalPct",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionWithSuperGoalPct",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionWithoutGoalPct",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultCpaValue",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "FtdBonusEvery",
                table: "CareerLevels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "FtdBonusValue",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FtdRateBase",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FtdRateWithGoal",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FtdRateWithSuperGoal",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FtdSuperbetRate",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GoalBonusValue",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GroupCommissionPer20Percent",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GroupCommissionPerPercent",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetRevenueFactor",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetRevenuePctNoGoal",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetRevenuePctWithGoal",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RevPct",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesBonusEvery",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesBonusValue",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesPctBase",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesPctWithGoal",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesPctWithSuperGoal",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SupFtdOtherNoGoal",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SupFtdOtherWithGoal",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SupFtdSuperbetNoGoal",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SupFtdSuperbetWithGoal",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SupRevPct",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SupSalesPctNoGoal",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SupSalesPctWithGoal",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrafficCpaBetFair",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrafficCpaBetMgm",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrafficCpaBetano",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrafficCpaBlaze",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrafficCpaEsportiva",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrafficCpaHiperbet",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrafficCpaNovibet",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrafficCpaStake",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrafficCpaSuperbet",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrafficInvestmentCommissionPct",
                table: "CareerLevels",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrafficSupBonus",
                table: "CareerLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_Name",
                table: "PaymentMethods",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "BetanoInternaValue",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "BetanoMundoBetValue",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "CommissionWithGoalPct",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "CommissionWithSuperGoalPct",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "CommissionWithoutGoalPct",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "DefaultCpaValue",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "FtdBonusEvery",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "FtdBonusValue",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "FtdRateBase",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "FtdRateWithGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "FtdRateWithSuperGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "FtdSuperbetRate",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "GoalBonusValue",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "GroupCommissionPer20Percent",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "GroupCommissionPerPercent",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "NetRevenueFactor",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "NetRevenuePctNoGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "NetRevenuePctWithGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "RevPct",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SalesBonusEvery",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SalesBonusValue",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SalesPctBase",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SalesPctWithGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SalesPctWithSuperGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SupFtdOtherNoGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SupFtdOtherWithGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SupFtdSuperbetNoGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SupFtdSuperbetWithGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SupRevPct",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SupSalesPctNoGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "SupSalesPctWithGoal",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "TrafficCpaBetFair",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "TrafficCpaBetMgm",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "TrafficCpaBetano",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "TrafficCpaBlaze",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "TrafficCpaEsportiva",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "TrafficCpaHiperbet",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "TrafficCpaNovibet",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "TrafficCpaStake",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "TrafficCpaSuperbet",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "TrafficInvestmentCommissionPct",
                table: "CareerLevels");

            migrationBuilder.DropColumn(
                name: "TrafficSupBonus",
                table: "CareerLevels");

            migrationBuilder.RenameColumn(
                name: "Client",
                table: "Projects",
                newName: "ClientName");

            migrationBuilder.RenameColumn(
                name: "TrafficSupCommissionPct",
                table: "CareerLevels",
                newName: "CommissionPercentage");
        }
    }
}
