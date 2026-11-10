using Microsoft.EntityFrameworkCore;
using MarksApi.Entities;

namespace MarksApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Student>(b =>
        {
            b.HasIndex(s => s.RegistrationNo).IsUnique();
            b.HasIndex(s => s.Email).IsUnique();
        });

        modelBuilder.Entity<Course>(b =>
        {
            b.HasIndex(c => c.Code).IsUnique();
            b.ToTable(t => t.HasCheckConstraint("CK_courses_credit_hours", "credit_hours BETWEEN 1 AND 4"));
        });

        modelBuilder.Entity<Enrollment>(b =>
        {
            b.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
            b.ToTable(t => t.HasCheckConstraint("CK_enrollments_mark", "mark IS NULL OR (mark BETWEEN 0 AND 100)"));

            b.HasOne(e => e.Student)
             .WithMany(s => s.Enrollments)
             .HasForeignKey(e => e.StudentId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Course)
             .WithMany(c => c.Enrollments)
             .HasForeignKey(e => e.CourseId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(u => u.Username).IsUnique();
            b.HasOne(u => u.Student)
             .WithMany()
             .HasForeignKey(u => u.StudentId)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}