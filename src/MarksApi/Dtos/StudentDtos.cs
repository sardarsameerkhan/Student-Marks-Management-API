namespace MarksApi.Dtos;

public record StudentDto(int Id, string RegistrationNo, string FullName, string Email, DateTime CreatedAtUtc);

public record CreateStudentRequest(string RegistrationNo, string FullName, string Email);

public record UpdateStudentRequest(string RegistrationNo, string FullName, string Email);