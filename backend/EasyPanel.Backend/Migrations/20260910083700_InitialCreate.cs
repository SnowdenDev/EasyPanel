using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyPanel.Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "audit_log_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    details_json = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    node_token_hash = table.Column<string>(type: "text", nullable: false),
                    connectivity_mode = table.Column<int>(type: "integer", nullable: false),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    last_heartbeat_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    daemon_version = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nodes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "port_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    protocol = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_port_allocations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staff_server_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    can_view_console = table.Column<bool>(type: "boolean", nullable: false),
                    can_send_console_input = table.Column<bool>(type: "boolean", nullable: false),
                    can_control_power = table.Column<bool>(type: "boolean", nullable: false),
                    can_access_file_manager = table.Column<bool>(type: "boolean", nullable: false),
                    can_edit_settings = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_server_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    work_directory = table.Column<string>(type: "text", nullable: false),
                    executable_relative_path = table.Column<string>(type: "text", nullable: false),
                    expected_executable_sha256 = table.Column<string>(type: "text", nullable: false),
                    launch_arguments = table.Column<string>(type: "text", nullable: true),
                    environment_variables_json = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    auto_restart_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    cpu_limit_percent = table.Column<int>(type: "integer", nullable: true),
                    memory_limit_megabytes = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instances", x => x.id);
                    table.ForeignKey(
                        name: "fk_instances_nodes_node_id",
                        column: x => x.node_id,
                        principalTable: "nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entries_actor_user_id_created_at_utc",
                table: "audit_log_entries",
                columns: new[] { "actor_user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entries_instance_id_created_at_utc",
                table: "audit_log_entries",
                columns: new[] { "instance_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_instances_node_id",
                table: "instances",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_port_allocations_node_id_port_protocol",
                table: "port_allocations",
                columns: new[] { "node_id", "port", "protocol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staff_server_permissions_user_id_instance_id",
                table: "staff_server_permissions",
                columns: new[] { "user_id", "instance_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log_entries");

            migrationBuilder.DropTable(
                name: "instances");

            migrationBuilder.DropTable(
                name: "port_allocations");

            migrationBuilder.DropTable(
                name: "staff_server_permissions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "nodes");
        }
    }
}
