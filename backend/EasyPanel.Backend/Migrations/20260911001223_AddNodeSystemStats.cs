using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyPanel.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeSystemStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "available_memory_megabytes",
                table: "nodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "cpu_usage_percent",
                table: "nodes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "host_name",
                table: "nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "logical_processor_count",
                table: "nodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "total_physical_memory_megabytes",
                table: "nodes",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available_memory_megabytes",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "cpu_usage_percent",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "host_name",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "logical_processor_count",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "total_physical_memory_megabytes",
                table: "nodes");
        }
    }
}
