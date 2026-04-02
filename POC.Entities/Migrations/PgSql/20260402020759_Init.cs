using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using POC.Entities;

#nullable disable

namespace POC.Entities.Migrations.PgSql
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.criteria_types", "Inclusion,Exclusion,Mainevent");

            migrationBuilder.CreateTable(
                name: "trials",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trials", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "criterias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    type = table.Column<CriteriaTypes>(type: "criteria_types", nullable: false),
                    trial_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("trial_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_criterias_trials_trial_id",
                        column: x => x.trial_id,
                        principalTable: "trials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_criterias_trial_id",
                table: "criterias",
                column: "trial_id");

            migrationBuilder.CreateIndex(
                name: "unique_pgsql_criteria_trial_id_criteria_type",
                table: "criterias",
                columns: new[] { "type", "trial_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "criterias");

            migrationBuilder.DropTable(
                name: "trials");
        }
    }
}
