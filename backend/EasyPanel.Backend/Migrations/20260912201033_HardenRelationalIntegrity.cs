using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyPanel.Backend.Migrations
{
    /// <inheritdoc />
    public partial class HardenRelationalIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM staff_server_permissions
                WHERE NOT EXISTS (SELECT 1 FROM users WHERE users.id = staff_server_permissions.user_id)
                   OR NOT EXISTS (SELECT 1 FROM instances WHERE instances.id = staff_server_permissions.instance_id);

                DELETE FROM port_allocations
                WHERE NOT EXISTS (SELECT 1 FROM nodes WHERE nodes.id = port_allocations.node_id)
                   OR NOT EXISTS (SELECT 1 FROM instances WHERE instances.id = port_allocations.instance_id);

                UPDATE audit_log_entries
                SET actor_user_id = NULL
                WHERE actor_user_id IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM users WHERE users.id = audit_log_entries.actor_user_id);

                UPDATE audit_log_entries
                SET instance_id = NULL
                WHERE instance_id IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM instances WHERE instances.id = audit_log_entries.instance_id);

                UPDATE audit_log_entries
                SET node_id = NULL
                WHERE node_id IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM nodes WHERE nodes.id = audit_log_entries.node_id);
                """);

            migrationBuilder.CreateIndex(
                name: "ix_staff_server_permissions_instance_id",
                table: "staff_server_permissions",
                column: "instance_id");

            migrationBuilder.CreateIndex(
                name: "ix_port_allocations_instance_id",
                table: "port_allocations",
                column: "instance_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entries_node_id",
                table: "audit_log_entries",
                column: "node_id");

            migrationBuilder.AddForeignKey(
                name: "fk_audit_log_entries_instances_instance_id",
                table: "audit_log_entries",
                column: "instance_id",
                principalTable: "instances",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_audit_log_entries_nodes_node_id",
                table: "audit_log_entries",
                column: "node_id",
                principalTable: "nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_audit_log_entries_users_actor_user_id",
                table: "audit_log_entries",
                column: "actor_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_port_allocations_instances_instance_id",
                table: "port_allocations",
                column: "instance_id",
                principalTable: "instances",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_port_allocations_nodes_node_id",
                table: "port_allocations",
                column: "node_id",
                principalTable: "nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_staff_server_permissions_instances_instance_id",
                table: "staff_server_permissions",
                column: "instance_id",
                principalTable: "instances",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_staff_server_permissions_users_user_id",
                table: "staff_server_permissions",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_audit_log_entries_instances_instance_id",
                table: "audit_log_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_audit_log_entries_nodes_node_id",
                table: "audit_log_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_audit_log_entries_users_actor_user_id",
                table: "audit_log_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_port_allocations_instances_instance_id",
                table: "port_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_port_allocations_nodes_node_id",
                table: "port_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_staff_server_permissions_instances_instance_id",
                table: "staff_server_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_staff_server_permissions_users_user_id",
                table: "staff_server_permissions");

            migrationBuilder.DropIndex(
                name: "ix_staff_server_permissions_instance_id",
                table: "staff_server_permissions");

            migrationBuilder.DropIndex(
                name: "ix_port_allocations_instance_id",
                table: "port_allocations");

            migrationBuilder.DropIndex(
                name: "ix_audit_log_entries_node_id",
                table: "audit_log_entries");
        }
    }
}
