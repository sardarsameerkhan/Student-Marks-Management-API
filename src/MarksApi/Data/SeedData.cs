using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        AppDbContext db,
        IPasswordHasher<AppUser> passwordHasher,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
            return;

        var teacherPassword = configuration["Seed:TeacherPassword"]
            ?? throw new InvalidOperationException("Set Seed:TeacherPassword using user-secrets or an environment variable.");
        var studentPassword = configuration["Seed:StudentPassword"]
            ?? throw new InvalidOperationException("Set Seed:StudentPassword using user-secrets or an environment variable.");

        var students = new[]
        {
            new Student { RegistrationNo = "CS-2022-001", FullName = "Ali Raza", Email = "ali.raza@example.test", CreatedAtUtc = DateTime.UtcNow },
            new Student { RegistrationNo = "CS-2022-002", FullName = "Sara Ahmed", Email = "sara.ahmed@example.test", CreatedAtUtc = DateTime.UtcNow }
        };
        var courses = new[]
        {
            new Course { Code = "CS401", Title = "Advanced Programming", CreditHours = 3, CreatedAtUtc = DateTime.UtcNow },
            new Course { Code = "MA201", Title = "Applied Mathematics", CreditHours = 2, CreatedAtUtc = DateTime.UtcNow },
            new Course { Code = "DB301", Title = "Database Systems", CreditHours = 3, CreatedAtUtc = DateTime.UtcNow }
        };
        db.Students.AddRange(students);
        db.Courses.AddRange(courses);
        await db.SaveChangesAsync(cancellationToken);

        db.Enrollments.AddRange(
            new Enrollment { StudentId = students[0].Id, CourseId = courses[0].Id, Mark = 80, EnrolledAtUtc = DateTime.UtcNow },
            new Enrollment { StudentId = students[0].Id, CourseId = courses[1].Id, Mark = 60, EnrolledAtUtc = DateTime.UtcNow },
            new Enrollment { StudentId = students[0].Id, CourseId = courses[2].Id, Mark = null, EnrolledAtUtc = DateTime.UtcNow },
            new Enrollment { StudentId = students[1].Id, CourseId = courses[0].Id, Mark = 70, EnrolledAtUtc = DateTime.UtcNow },
            new Enrollment { StudentId = students[1].Id, CourseId = courses[1].Id, Mark = null, EnrolledAtUtc = DateTime.UtcNow },
            new Enrollment { StudentId = students[1].Id, CourseId = courses[2].Id, Mark = 90, EnrolledAtUtc = DateTime.UtcNow });
        var teacher = new AppUser { Username = "teacher1", Role = "Teacher", PasswordHash = "" };
        teacher.PasswordHash = passwordHasher.HashPassword(teacher, teacherPassword);
        var firstStudentUser = new AppUser
        {
            Username = "student1", Role = "Student", StudentId = students[0].Id, PasswordHash = ""
        };
        firstStudentUser.PasswordHash = passwordHasher.HashPassword(firstStudentUser, studentPassword);
        var secondStudentUser = new AppUser
        {
            Username = "student2", Role = "Student", StudentId = students[1].Id, PasswordHash = ""
        };
        secondStudentUser.PasswordHash = passwordHasher.HashPassword(secondStudentUser, studentPassword);
        db.Users.AddRange(teacher, firstStudentUser, secondStudentUser);
        await db.SaveChangesAsync(cancellationToken);
    }
}
