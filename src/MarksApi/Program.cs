using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MarksApi.Data;
using MarksApi.Dtos;
using MarksApi.Middleware;
using MarksApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database"))
           .UseSnakeCaseNamingConvention());

// 2. Custom Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// 3. Exception Handling (RFC 7807 Problem Details)
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails();

// 4. JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware Pipeline
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

// Root Endpoint
app.MapGet("/", () => "Student Marks Management API is running!");

// Authentication Endpoints
app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
{
    var response = await authService.LoginAsync(request, ct);
    return response is null 
        ? Results.Problem(statusCode: 401, title: "Invalid username or password") 
        : Results.Ok(response);
});

// Student Endpoints
var studentGroup = app.MapGroup("/api/students").RequireAuthorization();

studentGroup.MapGet("/", async (IStudentService studentService, CancellationToken ct) =>
    Results.Ok(await studentService.GetAllAsync(ct)));

studentGroup.MapGet("/{id:int}", async (int id, IStudentService studentService, CancellationToken ct) =>
{
    var student = await studentService.GetByIdAsync(id, ct);
    return student is not null ? Results.Ok(student) : Results.NotFound();
});

studentGroup.MapPost("/", async (CreateStudentRequest request, IStudentService studentService, CancellationToken ct) =>
{
    var student = await studentService.CreateAsync(request, ct);
    return Results.Created($"/api/students/{student.Id}", student);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

studentGroup.MapPut("/{id:int}", async (int id, UpdateStudentRequest request, IStudentService studentService, CancellationToken ct) =>
{
    var student = await studentService.UpdateAsync(id, request, ct);
    return student is not null ? Results.Ok(student) : Results.NotFound();
}).RequireAuthorization(p => p.RequireRole("Teacher"));

studentGroup.MapDelete("/{id:int}", async (int id, IStudentService studentService, CancellationToken ct) =>
{
    var success = await studentService.DeleteAsync(id, ct);
    return success ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization(p => p.RequireRole("Teacher"));

studentGroup.MapGet("/{id:int}/marks", async (int id, IEnrollmentService enrollmentService, CancellationToken ct) =>
    Results.Ok(await enrollmentService.GetStudentEnrollmentsAsync(id, ct)));

// Course Endpoints
var courseGroup = app.MapGroup("/api/courses").RequireAuthorization();

courseGroup.MapGet("/", async (ICourseService courseService, CancellationToken ct) =>
    Results.Ok(await courseService.GetAllAsync(ct)));

courseGroup.MapGet("/{id:int}", async (int id, ICourseService courseService, CancellationToken ct) =>
{
    var course = await courseService.GetByIdAsync(id, ct);
    return course is not null ? Results.Ok(course) : Results.NotFound();
});

courseGroup.MapPost("/", async (CreateCourseRequest request, ICourseService courseService, CancellationToken ct) =>
{
    var course = await courseService.CreateAsync(request, ct);
    return Results.Created($"/api/courses/{course.Id}", course);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

courseGroup.MapPut("/{id:int}", async (int id, UpdateCourseRequest request, ICourseService courseService, CancellationToken ct) =>
{
    var course = await courseService.UpdateAsync(id, request, ct);
    return course is not null ? Results.Ok(course) : Results.NotFound();
}).RequireAuthorization(p => p.RequireRole("Teacher"));

courseGroup.MapDelete("/{id:int}", async (int id, ICourseService courseService, CancellationToken ct) =>
{
    var success = await courseService.DeleteAsync(id, ct);
    return success ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization(p => p.RequireRole("Teacher"));

// Enrollment & Marks Endpoints
courseGroup.MapPost("/{id:int}/enrollments", async (int id, CreateEnrollmentRequest request, IEnrollmentService enrollmentService, CancellationToken ct) =>
{
    var result = await enrollmentService.EnrollStudentAsync(id, request, ct);
    return result is not null 
        ? Results.Created($"/api/courses/{id}/enrollments", result) 
        : Results.BadRequest("Invalid course/student or student already enrolled.");
}).RequireAuthorization(p => p.RequireRole("Teacher"));

courseGroup.MapGet("/{id:int}/enrollments", async (int id, IEnrollmentService enrollmentService, CancellationToken ct) =>
    Results.Ok(await enrollmentService.GetCourseEnrollmentsAsync(id, ct)))
    .RequireAuthorization(p => p.RequireRole("Teacher"));

courseGroup.MapPut("/{id:int}/students/{studentId:int}/marks", async (int id, int studentId, RecordMarkRequest request, IEnrollmentService enrollmentService, CancellationToken ct) =>
{
    var result = await enrollmentService.RecordMarkAsync(id, studentId, request, ct);
    return result is not null ? Results.Ok(result) : Results.NotFound("Enrollment record not found.");
}).RequireAuthorization(p => p.RequireRole("Teacher"));

// Database Seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.SeedAsync(context);
}

app.Run();