namespace MarksApi.Dtos;

public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, string Role);
public sealed record StudentResponse(int Id, string RegistrationNo, string FullName, string Email);
public sealed record CourseResponse(int Id, string Code, string Title, int CreditHours);
public sealed record EnrollmentResponse(int Id, int StudentId, int CourseId, int? Mark);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record CourseStatsResponse(
    int CourseId, string CourseCode, int Enrolled, int Graded,
    double? Average, int? Highest, int? Lowest, int Passed);
public sealed record TranscriptCourse(string Code, int CreditHours, int? Mark, bool? Passed);
public sealed record TranscriptResponse(
    int StudentId, string RegistrationNo, string FullName,
    IReadOnlyList<TranscriptCourse> Courses, double? WeightedAverage);
