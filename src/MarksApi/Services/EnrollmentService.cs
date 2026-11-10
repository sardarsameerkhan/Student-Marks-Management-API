using Microsoft.EntityFrameworkCore;
using MarksApi.Data;
using MarksApi.Dtos;
using MarksApi.Entities;

namespace MarksApi.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly AppDbContext _context;

    public EnrollmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EnrollmentDto?> EnrollStudentAsync(int courseId, CreateEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == courseId, cancellationToken);
        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken);
        if (!courseExists || !studentExists) return null;

        var alreadyEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == request.StudentId, cancellationToken);
        if (alreadyEnrolled) return null;

        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync(cancellationToken);

        return new EnrollmentDto(enrollment.Id, enrollment.StudentId, enrollment.CourseId, enrollment.Mark, enrollment.EnrolledAtUtc);
    }

    public async Task<IEnumerable<EnrollmentDto>> GetCourseEnrollmentsAsync(int courseId, CancellationToken cancellationToken)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentDto(e.Id, e.StudentId, e.CourseId, e.Mark, e.EnrolledAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<EnrollmentDto?> RecordMarkAsync(int courseId, int studentId, RecordMarkRequest request, CancellationToken cancellationToken)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == studentId, cancellationToken);

        if (enrollment is null) return null;

        enrollment.Mark = request.Mark;
        await _context.SaveChangesAsync(cancellationToken);

        return new EnrollmentDto(enrollment.Id, enrollment.StudentId, enrollment.CourseId, enrollment.Mark, enrollment.EnrolledAtUtc);
    }

    public async Task<IEnumerable<EnrollmentDto>> GetStudentEnrollmentsAsync(int studentId, CancellationToken cancellationToken)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => new EnrollmentDto(e.Id, e.StudentId, e.CourseId, e.Mark, e.EnrolledAtUtc))
            .ToListAsync(cancellationToken);
    }
}