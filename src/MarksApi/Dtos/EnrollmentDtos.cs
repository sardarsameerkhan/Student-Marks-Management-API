namespace MarksApi.Dtos;

public record EnrollmentDto(int Id, int StudentId, int CourseId, int? Mark, DateTime EnrolledAtUtc);

public record CreateEnrollmentRequest(int StudentId);

public record RecordMarkRequest(int Mark);