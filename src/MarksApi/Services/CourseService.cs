using Microsoft.EntityFrameworkCore;
using MarksApi.Data;
using MarksApi.Dtos;
using MarksApi.Entities;

namespace MarksApi.Services;

public class CourseService : ICourseService
{
    private readonly AppDbContext _context;

    public CourseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CourseDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Courses
            .AsNoTracking()
            .Select(c => new CourseDto(c.Id, c.Code, c.Title, c.CreditHours, c.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CourseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return course is null 
            ? null 
            : new CourseDto(course.Id, course.Code, course.Title, course.CreditHours, course.CreatedAtUtc);
    }

    public async Task<CourseDto> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            CreditHours = request.CreditHours
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);

        return new CourseDto(course.Id, course.Code, course.Title, course.CreditHours, course.CreatedAtUtc);
    }

    public async Task<CourseDto?> UpdateAsync(int id, UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses.FindAsync(new object[] { id }, cancellationToken);
        if (course is null) return null;

        course.Code = request.Code;
        course.Title = request.Title;
        course.CreditHours = request.CreditHours;

        await _context.SaveChangesAsync(cancellationToken);

        return new CourseDto(course.Id, course.Code, course.Title, course.CreditHours, course.CreatedAtUtc);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var course = await _context.Courses.FindAsync(new object[] { id }, cancellationToken);
        if (course is null) return false;

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}