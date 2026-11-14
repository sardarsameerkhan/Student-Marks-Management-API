using MarksApi.Dtos;

namespace MarksApi.Services;

public interface ICourseService
{
    Task<PagedResponse<CourseResponse>> ListAsync(PageQuery query, CancellationToken cancellationToken);
    Task<CourseResponse> GetAsync(int id, CancellationToken cancellationToken);
    Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken);
    Task<CourseResponse> UpdateAsync(int id, UpdateCourseRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<EnrollmentResponse> EnrollAsync(int courseId, CreateEnrollmentRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<EnrollmentResponse>> EnrollmentsAsync(int courseId, PageQuery query, CancellationToken cancellationToken);
    Task<EnrollmentResponse> UpdateMarkAsync(int enrollmentId, UpdateMarkRequest request, CancellationToken cancellationToken);
    Task DeleteEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken);
    Task<CourseStatsResponse> StatsAsync(int courseId, CancellationToken cancellationToken);
}
