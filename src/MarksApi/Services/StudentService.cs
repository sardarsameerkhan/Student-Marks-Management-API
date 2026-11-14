using System.Security.Claims;
using MarksApi.Data;
using MarksApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Services;

public sealed class StudentService(
    AppDbContext db,
    IConfiguration configuration,
    ILogger<StudentService> logger) : IStudentService
{
    public async Task<PagedResponse<StudentResponse>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var students = db.Students.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            students = students.Where(x =>
                x.RegistrationNo.Contains(search) || x.FullName.Contains(search) || x.Email.Contains(search));
        }

        students = (query.Sort ?? "").ToLowerInvariant() switch
        {
            "registrationno" => students.OrderBy(x => x.RegistrationNo),
            "fullname" => students.OrderBy(x => x.FullName),
            "email" => students.OrderBy(x => x.Email),
            "createdatutc" => students.OrderBy(x => x.CreatedAtUtc),
            "-registrationno" => students.OrderByDescending(x => x.RegistrationNo),
            "-fullname" => students.OrderByDescending(x => x.FullName),
            "-email" => students.OrderByDescending(x => x.Email),
            "-createdatutc" => students.OrderByDescending(x => x.CreatedAtUtc),
            _ => students.OrderBy(x => x.Id)
        };

        var page = Math.Max(1, query.Page ?? 1);
        var pageSize = Math.Clamp(query.PageSize ?? 10, 1, 50);
        var total = await students.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        var items = await students.Skip(skip).Take(pageSize)
            .Select(x => new StudentResponse(x.Id, x.RegistrationNo, x.FullName, x.Email))
            .ToListAsync(cancellationToken);
        return new PagedResponse<StudentResponse>(items, page, pageSize, total);
    }

    public async Task<StudentResponse> GetAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        EnsureSelfOrTeacher(id, user);
        var result = await db.Students.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new StudentResponse(x.Id, x.RegistrationNo, x.FullName, x.Email))
            .SingleOrDefaultAsync(cancellationToken);
        return result ?? throw new ApiNotFoundException($"Student {id} was not found.");
    }

    public async Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var student = new Student
        {
            RegistrationNo = request.RegistrationNo.Trim(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Students.Add(student);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created student {StudentId} with registration number {RegistrationNo}",
            student.Id, student.RegistrationNo);
        return new StudentResponse(student.Id, student.RegistrationNo, student.FullName, student.Email);
    }

    public async Task<StudentResponse> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken cancellationToken)
    {
        var student = await db.Students.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiNotFoundException($"Student {id} was not found.");
        student.RegistrationNo = request.RegistrationNo.Trim();
        student.FullName = request.FullName.Trim();
        student.Email = request.Email.Trim().ToLowerInvariant();
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated student {StudentId}", id);
        return new StudentResponse(student.Id, student.RegistrationNo, student.FullName, student.Email);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var student = await db.Students.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiNotFoundException($"Student {id} was not found.");
        if (await db.Enrollments.AnyAsync(x => x.StudentId == id, cancellationToken))
            throw new ApiConflictException("A student with enrollments cannot be deleted.");
        if (await db.Users.AnyAsync(x => x.StudentId == id, cancellationToken))
            throw new ApiConflictException("A student linked to a user account cannot be deleted.");
        db.Students.Remove(student);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted student {StudentId}", id);
    }

    public async Task<TranscriptResponse> TranscriptAsync(
        int id, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        EnsureSelfOrTeacher(id, user);
        var student = await db.Students.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.RegistrationNo, x.FullName })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiNotFoundException($"Student {id} was not found.");

        var courses = await db.Enrollments.AsNoTracking()
            .Where(x => x.StudentId == id)
            .OrderBy(x => x.Course.Code)
            .Select(x => new TranscriptCourse(
                x.Course.Code, x.Course.CreditHours, x.Mark,
                x.Mark == null ? null : x.Mark >= configuration.GetValue<int>("PassMark", 50)))
            .ToListAsync(cancellationToken);
        var graded = await db.Enrollments.AsNoTracking()
            .Where(x => x.StudentId == id && x.Mark != null)
            .Select(x => new { x.Mark, x.Course.CreditHours })
            .ToListAsync(cancellationToken);
        var totalCredits = graded.Sum(x => x.CreditHours);
        double? average = totalCredits == 0
            ? null
            : graded.Sum(x => x.Mark!.Value * x.CreditHours) / (double)totalCredits;
        return new TranscriptResponse(student.Id, student.RegistrationNo, student.FullName, courses, average);
    }

    private static void EnsureSelfOrTeacher(int id, ClaimsPrincipal user)
    {
        if (user.IsInRole("Teacher"))
            return;
        var studentId = user.FindFirstValue("studentId");
        if (!int.TryParse(studentId, out var ownId) || ownId != id)
            throw new ApiForbiddenException("Students may access only their own records.");
    }
}
