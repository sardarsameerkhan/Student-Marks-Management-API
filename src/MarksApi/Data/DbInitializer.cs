using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MarksApi.Entities;

namespace MarksApi.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Apply any pending migrations automatically on startup
        await context.Database.MigrateAsync();

        var passwordHasher = new PasswordHasher<User>();

        // 1. Seed Students
        if (!await context.Students.AnyAsync())
        {
            var student = new Student
            {
                RegistrationNo = "FA20-BSCS-001",
                FullName = "John Doe",
                Email = "john.doe@student.edu"
            };

            context.Students.Add(student);
            await context.SaveChangesAsync();

            // Seed Student User Account
            if (!await context.Users.AnyAsync(u => u.Username == "student"))
            {
                var studentUser = new User
                {
                    Username = "student",
                    Role = "Student",
                    StudentId = student.Id
                };
                studentUser.PasswordHash = passwordHasher.HashPassword(studentUser, "Student123!");
                context.Users.Add(studentUser);
            }
        }

        // 2. Seed Teacher User Account
        if (!await context.Users.AnyAsync(u => u.Username == "teacher"))
        {
            var teacherUser = new User
            {
                Username = "teacher",
                Role = "Teacher",
                StudentId = null
            };
            teacherUser.PasswordHash = passwordHasher.HashPassword(teacherUser, "Teacher123!");
            context.Users.Add(teacherUser);
        }

        // 3. Seed Sample Courses
        if (!await context.Courses.AnyAsync())
        {
            context.Courses.AddRange(
                new Course { Code = "CS101", Title = "Introduction to Programming", CreditHours = 3 },
                new Course { Code = "CS201", Title = "Database Systems", CreditHours = 4 }
            );
        }

        await context.SaveChangesAsync();
    }
}