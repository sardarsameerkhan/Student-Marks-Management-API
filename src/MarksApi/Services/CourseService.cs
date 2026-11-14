using MarksApi.Data;
using MarksApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Services;

public sealed class CourseService(
    AppDbContext db,
    IConfiguration configuration,
    ILogger<CourseService> logger) : ICourseService
{
    public async Task<PagedResponse<CourseResponse>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var courses = db.Courses.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            courses = courses.Where(x => x.Code.Contains(search) || x.Title.Contains(search));
        }
        courses = (query.Sort ?? "").ToLowerInvariant() switch
        {
            "code" => courses.OrderBy(x => x.Code),
            "title" => courses.OrderBy(x => x.Title),
            "credithours" => courses.OrderBy(x => x.CreditHours),
            "-code" => courses.OrderByDescending(x => x.Code),
            "-title" => courses.OrderByDescending(x => x.Title),
            "-credithours" => courses.OrderByDescending(x => x.CreditHours),
            _ => courses.OrderBy(x => x.Id)
        };
        var page = Math.Max(1, query.Page ?? 1);
        var pageSize = Math.Clamp(query.PageSize ?? 10, 1, 50);
        var total = await courses.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        var items = await courses.Skip(skip).Take(pageSize)
            .Select(x => new CourseResponse(x.Id, x.Code, x.Title, x.CreditHours))
            .ToListAsync(cancellationToken);
        return new PagedResponse<CourseResponse>(items, page, pageSize, total);
    }

    public async Task<CourseResponse> GetAsync(int id, CancellationToken cancellationToken)
    {
        var course = await db.Courses.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new CourseResponse(x.Id, x.Code, x.Title, x.CreditHours))
            .SingleOrDefaultAsync(cancellationToken);
        return course ?? throw new ApiNotFoundException($"Course {id} was not found.");
    }

    public async Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var course = new Course
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Title = request.Title.Trim(),
            CreditHours = request.CreditHours,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created course {CourseId} with code {CourseCode}", course.Id, course.Code);
        return new CourseResponse(course.Id, course.Code, course.Title, course.CreditHours);
    }

    public async Task<CourseResponse> UpdateAsync(int id, UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        var course = await db.Courses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiNotFoundException($"Course {id} was not found.");
        course.Code = request.Code.Trim().ToUpperInvariant();
        course.Title = request.Title.Trim();
        course.CreditHours = request.CreditHours;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated course {CourseId}", id);
        return new CourseResponse(course.Id, course.Code, course.Title, course.CreditHours);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var course = await db.Courses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiNotFoundException($"Course {id} was not found.");
        if (await db.Enrollments.AnyAsync(x => x.CourseId == id, cancellationToken))
            throw new ApiConflictException("A course with enrollments cannot be deleted.");
        db.Courses.Remove(course);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted course {CourseId}", id);
    }

    public async Task<EnrollmentResponse> EnrollAsync(
        int courseId, CreateEnrollmentRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Courses.AnyAsync(x => x.Id == courseId, cancellationToken))
            throw new ApiNotFoundException($"Course {courseId} was not found.");
        if (!await db.Students.AnyAsync(x => x.Id == request.StudentId, cancellationToken))
            throw new ApiNotFoundException($"Student {request.StudentId} was not found.");
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAtUtc = DateTime.UtcNow
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Enrolled student {StudentId} in course {CourseId}",
            enrollment.StudentId, enrollment.CourseId);
        return new EnrollmentResponse(enrollment.Id, enrollment.StudentId, enrollment.CourseId, enrollment.Mark);
    }

    public async Task<PagedResponse<EnrollmentResponse>> EnrollmentsAsync(
        int courseId, PageQuery query, CancellationToken cancellationToken)
    {
        if (!await db.Courses.AnyAsync(x => x.Id == courseId, cancellationToken))
            throw new ApiNotFoundException($"Course {courseId} was not found.");
        var enrollments = db.Enrollments.AsNoTracking().Where(x => x.CourseId == courseId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            enrollments = enrollments.Where(x =>
                x.Student.RegistrationNo.Contains(search) || x.Student.FullName.Contains(search) ||
                (x.Mark != null && x.Mark.Value.ToString() == search));
        }
        enrollments = (query.Sort ?? "").ToLowerInvariant() switch
        {
            "studentid" => enrollments.OrderBy(x => x.StudentId),
            "mark" => enrollments.OrderBy(x => x.Mark),
            "enrolledatutc" => enrollments.OrderBy(x => x.EnrolledAtUtc),
            "-studentid" => enrollments.OrderByDescending(x => x.StudentId),
            "-mark" => enrollments.OrderByDescending(x => x.Mark),
            "-enrolledatutc" => enrollments.OrderByDescending(x => x.EnrolledAtUtc),
            "-id" => enrollments.OrderByDescending(x => x.Id),
            _ => enrollments.OrderBy(x => x.Id)
        };
        var page = Math.Max(1, query.Page ?? 1);
        var pageSize = Math.Clamp(query.PageSize ?? 10, 1, 50);
        var total = await enrollments.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        var items = await enrollments.Skip(skip).Take(pageSize)
            .Select(x => new EnrollmentResponse(x.Id, x.StudentId, x.CourseId, x.Mark))
            .ToListAsync(cancellationToken);
        return new PagedResponse<EnrollmentResponse>(items, page, pageSize, total);
    }

    public async Task<EnrollmentResponse> UpdateMarkAsync(
        int enrollmentId, UpdateMarkRequest request, CancellationToken cancellationToken)
    {
        var enrollment = await db.Enrollments.SingleOrDefaultAsync(x => x.Id == enrollmentId, cancellationToken)
            ?? throw new ApiNotFoundException($"Enrollment {enrollmentId} was not found.");
        enrollment.Mark = request.Mark;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated mark for enrollment {EnrollmentId}", enrollmentId);
        return new EnrollmentResponse(enrollment.Id, enrollment.StudentId, enrollment.CourseId, enrollment.Mark);
    }

    public async Task DeleteEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken)
    {
        var enrollment = await db.Enrollments.SingleOrDefaultAsync(x => x.Id == enrollmentId, cancellationToken)
            ?? throw new ApiNotFoundException($"Enrollment {enrollmentId} was not found.");
        db.Enrollments.Remove(enrollment);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted enrollment {EnrollmentId}", enrollmentId);
    }

    public async Task<CourseStatsResponse> StatsAsync(int courseId, CancellationToken cancellationToken)
    {
        var course = await db.Courses.AsNoTracking().Where(x => x.Id == courseId)
            .Select(x => new { x.Id, x.Code })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiNotFoundException($"Course {courseId} was not found.");
        var marks = await db.Enrollments.AsNoTracking()
            .Where(x => x.CourseId == courseId && x.Mark != null)
            .Select(x => x.Mark!.Value)
            .ToListAsync(cancellationToken);
        var passed = marks.Count(x => x >= configuration.GetValue<int>("PassMark", 50));
        return new CourseStatsResponse(course.Id, course.Code,
            await db.Enrollments.CountAsync(x => x.CourseId == courseId, cancellationToken),
            marks.Count,
            marks.Count == 0 ? null : marks.Average(),
            marks.Count == 0 ? null : marks.Max(),
            marks.Count == 0 ? null : marks.Min(),
            passed);
    }
}
