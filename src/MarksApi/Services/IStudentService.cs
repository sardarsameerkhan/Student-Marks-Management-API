using System.Security.Claims;
using MarksApi.Dtos;

namespace MarksApi.Services;

public interface IStudentService
{
    Task<PagedResponse<StudentResponse>> ListAsync(PageQuery query, CancellationToken cancellationToken);
    Task<StudentResponse> GetAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken);
    Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken);
    Task<StudentResponse> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<TranscriptResponse> TranscriptAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken);
}
