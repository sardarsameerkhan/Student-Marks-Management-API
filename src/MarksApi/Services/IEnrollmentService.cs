using MarksApi.Dtos;

namespace MarksApi.Services;

public interface IEnrollmentService
{
    Task<EnrollmentDto?> EnrollStudentAsync(int courseId, CreateEnrollmentRequest request, CancellationToken cancellationToken);
    Task<IEnumerable<EnrollmentDto>> GetCourseEnrollmentsAsync(int courseId, CancellationToken cancellationToken);
    Task<EnrollmentDto?> RecordMarkAsync(int courseId, int studentId, RecordMarkRequest request, CancellationToken cancellationToken);
    Task<IEnumerable<EnrollmentDto>> GetStudentEnrollmentsAsync(int studentId, CancellationToken cancellationToken);
}