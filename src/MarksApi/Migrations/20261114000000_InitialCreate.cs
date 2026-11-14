using MarksApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarksApi.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261114000000_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "courses",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                credit_hours = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_courses", x => x.id);
                table.CheckConstraint("ck_courses_credit_hours", "credit_hours BETWEEN 1 AND 4");
            });

        migrationBuilder.CreateTable(
            name: "students",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                registration_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_students", x => x.id);
                table.CheckConstraint("ck_students_email_lowercase", "email = lower(email)");
            });

        migrationBuilder.CreateTable(
            name: "enrollments",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                student_id = table.Column<int>(type: "integer", nullable: false),
                course_id = table.Column<int>(type: "integer", nullable: false),
                mark = table.Column<int>(type: "integer", nullable: true),
                enrolled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_enrollments", x => x.id);
                table.CheckConstraint("ck_enrollments_mark", "mark IS NULL OR (mark >= 0 AND mark <= 100)");
                table.ForeignKey("fk_enrollments_courses_course_id", x => x.course_id, "courses", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_enrollments_students_student_id", x => x.student_id, "students", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                password_hash = table.Column<string>(type: "text", nullable: false),
                role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                student_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
                table.CheckConstraint("ck_users_role_student_link",
                    "(role = 'Teacher' AND student_id IS NULL) OR (role = 'Student' AND student_id IS NOT NULL)");
                table.ForeignKey("fk_users_students_student_id", x => x.student_id, "students", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("ix_courses_code", "courses", "code", unique: true);
        migrationBuilder.CreateIndex("ix_enrollments_course_id", "enrollments", "course_id");
        migrationBuilder.CreateIndex("ix_enrollments_student_id_course_id", "enrollments", new[] { "student_id", "course_id" }, unique: true);
        migrationBuilder.CreateIndex("ix_students_email", "students", "email", unique: true);
        migrationBuilder.CreateIndex("ix_students_registration_no", "students", "registration_no", unique: true);
        migrationBuilder.CreateIndex("ix_users_student_id", "users", "student_id", unique: true);
        migrationBuilder.CreateIndex("ix_users_username", "users", "username", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "enrollments");
        migrationBuilder.DropTable(name: "users");
        migrationBuilder.DropTable(name: "courses");
        migrationBuilder.DropTable(name: "students");
    }
}
