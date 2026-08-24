using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payroll");

            migrationBuilder.CreateTable(
                name: "advances",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    given_date = table.Column<DateOnly>(type: "date", nullable: false),
                    recovery_start_month = table.Column<DateOnly>(type: "date", nullable: false),
                    installment_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_installments = table.Column<int>(type: "integer", nullable: false),
                    installments_recovered = table.Column<int>(type: "integer", nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_advances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attendance_records",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendance_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_present = table.Column<bool>(type: "boolean", nullable: false),
                    entered_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changes = table.Column<string>(type: "jsonb", nullable: false),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_division_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    staff_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    bank_account_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ifsc_code = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    date_of_joining = table.Column<DateOnly>(type: "date", nullable: false),
                    employment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pf_opted_in = table.Column<bool>(type: "boolean", nullable: false),
                    pf_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pf_active_from = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payroll_runs",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_division_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_month = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    plan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "free"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "advance_installments",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    advance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    due_month = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payroll_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_advance_installments", x => x.id);
                    table.ForeignKey(
                        name: "fk_advance_installments_advances_advance_id",
                        column: x => x.advance_id,
                        principalSchema: "payroll",
                        principalTable: "advances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_pf_configs",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pf_gross = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_pf_configs", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_pf_configs_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "payroll",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_salary_components",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    component_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_salary_components", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_salary_components_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "payroll",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payroll_entries",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    salary_after_pf = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    pf_gross = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_pf_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    lop_days = table.Column<int>(type: "integer", nullable: false),
                    lop_deduction = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    gross_salary = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_deductions = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_additions = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    nett_pay = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    esi_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    nett_salary = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    hr_entered_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    amount_matches = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_payroll_entries_payroll_runs_payroll_run_id",
                        column: x => x.payroll_run_id,
                        principalSchema: "payroll",
                        principalTable: "payroll_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schools",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schools", x => x.id);
                    table.ForeignKey(
                        name: "fk_schools_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "payroll",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "payroll",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payroll_additions",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    addition_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_additions", x => x.id);
                    table.ForeignKey(
                        name: "fk_payroll_additions_payroll_entries_payroll_entry_id",
                        column: x => x.payroll_entry_id,
                        principalSchema: "payroll",
                        principalTable: "payroll_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payroll_deductions",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deduction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_deductions", x => x.id);
                    table.ForeignKey(
                        name: "fk_payroll_deductions_payroll_entries_payroll_entry_id",
                        column: x => x.payroll_entry_id,
                        principalSchema: "payroll",
                        principalTable: "payroll_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "school_divisions",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_school_divisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_school_divisions_schools_school_id",
                        column: x => x.school_id,
                        principalSchema: "payroll",
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "school_payroll_settings",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employer_pf_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    esi_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_school_payroll_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_school_payroll_settings_schools_school_id",
                        column: x => x.school_id,
                        principalSchema: "payroll",
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_advance_installments_advance_id_due_month",
                schema: "payroll",
                table: "advance_installments",
                columns: new[] { "advance_id", "due_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_employee_id_attendance_date",
                schema: "payroll",
                table: "attendance_records",
                columns: new[] { "employee_id", "attendance_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id",
                schema: "payroll",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_pf_configs_employee_id",
                schema: "payroll",
                table: "employee_pf_configs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_salary_components_employee_id",
                schema: "payroll",
                table: "employee_salary_components",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_additions_payroll_entry_id",
                schema: "payroll",
                table: "payroll_additions",
                column: "payroll_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_deductions_payroll_entry_id",
                schema: "payroll",
                table: "payroll_deductions",
                column: "payroll_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_entries_payroll_run_id_employee_id",
                schema: "payroll",
                table: "payroll_entries",
                columns: new[] { "payroll_run_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_school_division_id_payroll_month",
                schema: "payroll",
                table: "payroll_runs",
                columns: new[] { "school_division_id", "payroll_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                schema: "payroll",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                schema: "payroll",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_school_divisions_school_id_name",
                schema: "payroll",
                table: "school_divisions",
                columns: new[] { "school_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_school_payroll_settings_school_id",
                schema: "payroll",
                table: "school_payroll_settings",
                column: "school_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_schools_tenant_id",
                schema: "payroll",
                table: "schools",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "payroll",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "advance_installments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "attendance_records",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "employee_pf_configs",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "employee_salary_components",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "payroll_additions",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "payroll_deductions",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "school_divisions",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "school_payroll_settings",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "advances",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "payroll_entries",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "users",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "schools",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "payroll_runs",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "payroll");
        }
    }
}
