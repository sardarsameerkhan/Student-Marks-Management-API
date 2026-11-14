namespace MarksApi.Data;

public sealed class Student
{
    public int Id { get; set; }
    public required string RegistrationNo { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

public sealed class Course
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Title { get; set; }
    public int CreditHours { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

public sealed class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int? Mark { get; set; }
    public DateTime EnrolledAtUtc { get; set; }
}

public sealed class AppUser
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
    public int? StudentId { get; set; }
    public Student? Student { get; set; }
}
