using Microsoft.EntityFrameworkCore;

namespace MarksApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("students", table => table.HasCheckConstraint(
                "ck_students_email_lowercase", "email = lower(email)"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RegistrationNo).HasMaxLength(20).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.RegistrationNo).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("courses", table => table.HasCheckConstraint(
                "ck_courses_credit_hours", "credit_hours BETWEEN 1 AND 4"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(150).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("enrollments", table => table.HasCheckConstraint(
                "ck_enrollments_mark", "mark IS NULL OR (mark >= 0 AND mark <= 100)"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EnrolledAtUtc).IsRequired();
            entity.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
            entity.HasOne(x => x.Student).WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Course).WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users", table => table.HasCheckConstraint(
                "ck_users_role_student_link",
                "(role = 'Teacher' AND student_id IS NULL) OR (role = 'Student' AND student_id IS NOT NULL)"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.StudentId).IsUnique();
            entity.HasOne(x => x.Student).WithOne()
                .HasForeignKey<AppUser>(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
