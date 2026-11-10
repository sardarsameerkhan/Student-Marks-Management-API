using MarksApi.Dtos;

namespace MarksApi.Services;

public interface IStudentService
{
    Task<IEnumerable<StudentDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<StudentDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<StudentDto> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken);
    Task<StudentDto?> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}