using System.ComponentModel.DataAnnotations;

namespace MarksApi.Dtos;

public sealed class LoginRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Username { get; init; } = "";
    [Required, StringLength(200, MinimumLength = 1)]
    public string Password { get; init; } = "";
}

public class CreateStudentRequest
{
    [Required, StringLength(20, MinimumLength = 1)]
    public string RegistrationNo { get; init; } = "";
    [Required, StringLength(100, MinimumLength = 1)]
    public string FullName { get; init; } = "";
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; init; } = "";
}

public sealed class UpdateStudentRequest : CreateStudentRequest { }

public class CreateCourseRequest
{
    [Required, StringLength(10, MinimumLength = 1)]
    public string Code { get; init; } = "";
    [Required, StringLength(150, MinimumLength = 1)]
    public string Title { get; init; } = "";
    [Range(1, 4)]
    public int CreditHours { get; init; }
}

public sealed class UpdateCourseRequest : CreateCourseRequest { }

public sealed class CreateEnrollmentRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; init; }
}

public sealed class UpdateMarkRequest
{
    [Required, Range(0, 100)]
    public int? Mark { get; init; }
}

public sealed class PageQuery
{
    [Range(1, int.MaxValue)]
    public int? Page { get; init; } = 1;
    [Range(1, 50)]
    public int? PageSize { get; init; } = 10;
    [StringLength(100)]
    public string? Search { get; init; }
    [StringLength(30)]
    public string? Sort { get; init; }
}
