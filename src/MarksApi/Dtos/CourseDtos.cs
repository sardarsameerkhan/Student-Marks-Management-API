namespace MarksApi.Dtos;

public record CourseDto(int Id, string Code, string Title, int CreditHours, DateTime CreatedAtUtc);

public record CreateCourseRequest(string Code, string Title, int CreditHours);

public record UpdateCourseRequest(string Code, string Title, int CreditHours);