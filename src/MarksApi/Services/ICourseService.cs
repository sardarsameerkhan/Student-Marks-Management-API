using MarksApi.Dtos;

namespace MarksApi.Services;

public interface ICourseService
{
    Task<IEnumerable<CourseDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CourseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<CourseDto> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken);
    Task<CourseDto?> UpdateAsync(int id, UpdateCourseRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}