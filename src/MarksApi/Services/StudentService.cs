using Microsoft.EntityFrameworkCore;
using MarksApi.Data;
using MarksApi.Dtos;
using MarksApi.Entities;

namespace MarksApi.Services;

public class StudentService : IStudentService
{
    private readonly AppDbContext _context;

    public StudentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StudentDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Students
            .AsNoTracking()
            .Select(s => new StudentDto(s.Id, s.RegistrationNo, s.FullName, s.Email, s.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<StudentDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return student is null 
            ? null 
            : new StudentDto(student.Id, student.RegistrationNo, student.FullName, student.Email, student.CreatedAtUtc);
    }

    public async Task<StudentDto> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var student = new Student
        {
            RegistrationNo = request.RegistrationNo,
            FullName = request.FullName,
            Email = request.Email
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync(cancellationToken);

        return new StudentDto(student.Id, student.RegistrationNo, student.FullName, student.Email, student.CreatedAtUtc);
    }

    public async Task<StudentDto?> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken cancellationToken)
    {
        var student = await _context.Students.FindAsync(new object[] { id }, cancellationToken);
        if (student is null) return null;

        student.RegistrationNo = request.RegistrationNo;
        student.FullName = request.FullName;
        student.Email = request.Email;

        await _context.SaveChangesAsync(cancellationToken);

        return new StudentDto(student.Id, student.RegistrationNo, student.FullName, student.Email, student.CreatedAtUtc);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var student = await _context.Students.FindAsync(new object[] { id }, cancellationToken);
        if (student is null) return false;

        _context.Students.Remove(student);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}