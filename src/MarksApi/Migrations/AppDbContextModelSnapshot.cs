using MarksApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarksApi.Migrations;

[DbContext(typeof(AppDbContext))]
public sealed class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("MarksApi.Data.AppUser", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").HasColumnName("id")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<string>("PasswordHash").IsRequired().HasColumnType("text").HasColumnName("password_hash");
            b.Property<string>("Role").IsRequired().HasMaxLength(20).HasColumnType("character varying(20)").HasColumnName("role");
            b.Property<int?>("StudentId").HasColumnType("integer").HasColumnName("student_id");
            b.Property<string>("Username").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)").HasColumnName("username");
            b.HasKey("Id").HasName("pk_users");
            b.HasIndex("StudentId").IsUnique().HasDatabaseName("ix_users_student_id");
            b.HasIndex("Username").IsUnique().HasDatabaseName("ix_users_username");
            b.ToTable("users", t => t.HasCheckConstraint("ck_users_role_student_link",
                "(role = 'Teacher' AND student_id IS NULL) OR (role = 'Student' AND student_id IS NOT NULL)"));
        });

        modelBuilder.Entity("MarksApi.Data.Course", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").HasColumnName("id")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<string>("Code").IsRequired().HasMaxLength(10).HasColumnType("character varying(10)").HasColumnName("code");
            b.Property<int>("CreditHours").HasColumnType("integer").HasColumnName("credit_hours");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("Title").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)").HasColumnName("title");
            b.HasKey("Id").HasName("pk_courses");
            b.HasIndex("Code").IsUnique().HasDatabaseName("ix_courses_code");
            b.ToTable("courses", t => t.HasCheckConstraint("ck_courses_credit_hours", "credit_hours BETWEEN 1 AND 4"));
        });

        modelBuilder.Entity("MarksApi.Data.Student", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").HasColumnName("id")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("Email").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)").HasColumnName("email");
            b.Property<string>("FullName").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)").HasColumnName("full_name");
            b.Property<string>("RegistrationNo").IsRequired().HasMaxLength(20).HasColumnType("character varying(20)").HasColumnName("registration_no");
            b.HasKey("Id").HasName("pk_students");
            b.HasIndex("Email").IsUnique().HasDatabaseName("ix_students_email");
            b.HasIndex("RegistrationNo").IsUnique().HasDatabaseName("ix_students_registration_no");
            b.ToTable("students", t => t.HasCheckConstraint("ck_students_email_lowercase", "email = lower(email)"));
        });

        modelBuilder.Entity("MarksApi.Data.Enrollment", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").HasColumnName("id")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<DateTime>("EnrolledAtUtc").HasColumnType("timestamp with time zone").HasColumnName("enrolled_at_utc");
            b.Property<int>("CourseId").HasColumnType("integer").HasColumnName("course_id");
            b.Property<int?>("Mark").HasColumnType("integer").HasColumnName("mark");
            b.Property<int>("StudentId").HasColumnType("integer").HasColumnName("student_id");
            b.HasKey("Id").HasName("pk_enrollments");
            b.HasIndex("CourseId").HasDatabaseName("ix_enrollments_course_id");
            b.HasIndex("StudentId", "CourseId").IsUnique().HasDatabaseName("ix_enrollments_student_id_course_id");
            b.ToTable("enrollments", t => t.HasCheckConstraint(
                "ck_enrollments_mark", "mark IS NULL OR (mark >= 0 AND mark <= 100)"));
        });

        modelBuilder.Entity("MarksApi.Data.AppUser", b =>
        {
            b.HasOne("MarksApi.Data.Student", "Student").WithOne()
                .HasForeignKey("MarksApi.Data.AppUser", "StudentId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_users_students_student_id");
        });
        modelBuilder.Entity("MarksApi.Data.Enrollment", b =>
        {
            b.HasOne("MarksApi.Data.Course", "Course").WithMany("Enrollments")
                .HasForeignKey("CourseId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_enrollments_courses_course_id");
            b.HasOne("MarksApi.Data.Student", "Student").WithMany("Enrollments")
                .HasForeignKey("StudentId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_enrollments_students_student_id");
        });
        modelBuilder.Entity("MarksApi.Data.Course", b => b.Navigation("Enrollments"));
        modelBuilder.Entity("MarksApi.Data.Student", b => b.Navigation("Enrollments"));
        modelBuilder.Entity("MarksApi.Data.AppUser", b => b.Navigation("Student"));
        modelBuilder.Entity("MarksApi.Data.Enrollment", b =>
        {
            b.Navigation("Course");
            b.Navigation("Student");
        });
    }
}
