using MarksApi.Dtos;
using MarksApi.Infrastructure;
using MarksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarksApi;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", async (
            LoginRequest request, IAuthService service, CancellationToken ct) =>
                Results.Ok(await service.LoginAsync(request, ct)))
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .AllowAnonymous()
            .WithTags("Auth");

        var students = app.MapGroup("/api/students")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,Student" })
            .WithTags("Students");
        students.MapGet("/", async (
            [AsParameters] PageQuery query, IStudentService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(query, ct)))
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });
        students.MapGet("/{id:int}", async (
            int id, HttpContext context, IStudentService service, CancellationToken ct) =>
                Results.Ok(await service.GetAsync(id, context.User, ct)))
            .WithName("GetStudent");
        students.MapPost("/", async (
            CreateStudentRequest request, IStudentService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(request, ct);
                return Results.CreatedAtRoute("GetStudent", new { id = created.Id }, created);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" })
            .AddEndpointFilter<ValidationFilter<CreateStudentRequest>>();
        students.MapPut("/{id:int}", async (
            int id, UpdateStudentRequest request, IStudentService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(id, request, ct)))
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" })
            .AddEndpointFilter<ValidationFilter<UpdateStudentRequest>>();
        students.MapDelete("/{id:int}", async (
            int id, IStudentService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });
        students.MapGet("/{id:int}/transcript", async (
            int id, HttpContext context, IStudentService service, CancellationToken ct) =>
                Results.Ok(await service.TranscriptAsync(id, context.User, ct)));

        var courses = app.MapGroup("/api/courses")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,Student" })
            .WithTags("Courses");
        courses.MapGet("/", async (
            [AsParameters] PageQuery query, ICourseService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(query, ct)));
        courses.MapGet("/{id:int}", async (
            int id, ICourseService service, CancellationToken ct) =>
                Results.Ok(await service.GetAsync(id, ct)))
            .WithName("GetCourse");
        courses.MapPost("/", async (
            CreateCourseRequest request, ICourseService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(request, ct);
                return Results.CreatedAtRoute("GetCourse", new { id = created.Id }, created);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" })
            .AddEndpointFilter<ValidationFilter<CreateCourseRequest>>();
        courses.MapPut("/{id:int}", async (
            int id, UpdateCourseRequest request, ICourseService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(id, request, ct)))
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" })
            .AddEndpointFilter<ValidationFilter<UpdateCourseRequest>>();
        courses.MapDelete("/{id:int}", async (
            int id, ICourseService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });
        courses.MapPost("/{courseId:int}/enrollments", async (
            int courseId, CreateEnrollmentRequest request, ICourseService service, CancellationToken ct) =>
            {
                var created = await service.EnrollAsync(courseId, request, ct);
                return Results.Created($"/api/enrollments/{created.Id}", created);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" })
            .AddEndpointFilter<ValidationFilter<CreateEnrollmentRequest>>();
        courses.MapGet("/{courseId:int}/enrollments", async (
            int courseId, [AsParameters] PageQuery query, ICourseService service, CancellationToken ct) =>
                Results.Ok(await service.EnrollmentsAsync(courseId, query, ct)))
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });
        courses.MapGet("/{courseId:int}/stats", async (
            int courseId, ICourseService service, CancellationToken ct) =>
                Results.Ok(await service.StatsAsync(courseId, ct)))
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        var enrollments = app.MapGroup("/api/enrollments")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" })
            .WithTags("Enrollments and Marks");
        enrollments.MapPut("/{id:int}/mark", async (
            int id, UpdateMarkRequest request, ICourseService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateMarkAsync(id, request, ct)))
            .AddEndpointFilter<ValidationFilter<UpdateMarkRequest>>();
        enrollments.MapDelete("/{id:int}", async (
            int id, ICourseService service, CancellationToken ct) =>
            {
                await service.DeleteEnrollmentAsync(id, ct);
                return Results.NoContent();
            });

        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                if (report.Status != Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = StatusCodes.Status503ServiceUnavailable,
                        Title = "Service Unavailable",
                        Detail = "The API or its database dependency is unavailable."
                    }, context.RequestAborted);
                    return;
                }
                await context.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new { name = x.Key, status = x.Value.Status.ToString() })
                }, context.RequestAborted);
            }
        }).AllowAnonymous().WithTags("Health");
    }
}
