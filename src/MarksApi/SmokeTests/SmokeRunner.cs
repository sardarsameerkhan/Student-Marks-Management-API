using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace MarksApi.SmokeTests;

public sealed class SmokeRunner(IConfiguration configuration, ILogger<SmokeRunner> logger)
{
    private int failures;

    public async Task<int> RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var addresses = services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;
        var baseAddress = addresses?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(baseAddress))
            throw new InvalidOperationException("The smoke-test host did not publish a listening address.");
        var teacherPassword = configuration["Seed:TeacherPassword"]
            ?? throw new InvalidOperationException("Seed:TeacherPassword is required for smoke tests.");
        var studentPassword = configuration["Seed:StudentPassword"]
            ?? throw new InvalidOperationException("Seed:StudentPassword is required for smoke tests.");

        using var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
        var teacherToken = await LoginAsync(client, "teacher1", teacherPassword, cancellationToken);
        var studentToken = await LoginAsync(client, "student1", studentPassword, cancellationToken);
        if (string.IsNullOrEmpty(teacherToken) || string.IsNullOrEmpty(studentToken))
        {
            Console.WriteLine($"SMOKE TEST SUMMARY: FAIL ({failures} failed checks)");
            return 1;
        }

        var suffix = Guid.NewGuid().ToString("N");
        var registrationNo = $"SMOKE-{suffix[..12]}";
        var email = $"smoke-{suffix}@example.test";
        var courseCode = $"S{suffix[..9]}";
        var seedStudentId = 0;
        var studentId = 0;
        var courseId = 0;
        var enrollmentId = 0;

        try
        {
            await CheckAsync(client, "GET", "/health", HttpStatusCode.OK, null, null, cancellationToken);
            await CheckAsync(client, "GET", "/api/students", HttpStatusCode.OK, teacherToken, null, cancellationToken);
            await CheckAsync(client, "GET", "/api/students", HttpStatusCode.Forbidden, studentToken, null, cancellationToken);
            await CheckAsync(client, "GET", "/api/students/2147483647", HttpStatusCode.NotFound, teacherToken, null, cancellationToken);
            await CheckAsync(client, "GET", "/api/courses", HttpStatusCode.Unauthorized, null, null, cancellationToken);
            await CheckAsync(client, "POST", "/api/auth/login", HttpStatusCode.Unauthorized, null,
                new { username = "teacher1", password = "invalid-smoke-password" }, cancellationToken);

            var studentsResponse = await SendAsync(client, "GET", "/api/students?page=1&pageSize=50", teacherToken, null, cancellationToken);
            CheckResponse("GET", "/api/students?page=1&pageSize=50", HttpStatusCode.OK,
                studentsResponse.StatusCode, studentsResponse.Body);
            using (var document = JsonDocument.Parse(studentsResponse.Body))
            {
                var seededStudent = document.RootElement.GetProperty("items").EnumerateArray()
                    .FirstOrDefault(x => x.GetProperty("registrationNo").GetString() == "CS-2022-001");
                if (seededStudent.ValueKind != JsonValueKind.Undefined)
                    seedStudentId = seededStudent.GetProperty("id").GetInt32();
            }
            if (seedStudentId == 0)
                throw new InvalidOperationException("The seeded student CS-2022-001 was not returned by the API.");

            await CheckAsync(client, "GET", $"/api/students/{seedStudentId}", HttpStatusCode.OK, teacherToken, null, cancellationToken);
            await CheckAsync(client, "GET", $"/api/students/{seedStudentId}", HttpStatusCode.OK, studentToken, null, cancellationToken);
            await CheckAsync(client, "GET", $"/api/students/{seedStudentId}/transcript", HttpStatusCode.OK, studentToken, null, cancellationToken);
            await CheckAsync(client, "GET", $"/api/students/2147483647/transcript", HttpStatusCode.Forbidden, studentToken, null, cancellationToken);
            await CheckAsync(client, "POST", "/api/students", HttpStatusCode.BadRequest, teacherToken,
                new { registrationNo = "", fullName = "", email = "not-an-email" }, cancellationToken);

            var createdStudent = await SendAsync(client, "POST", "/api/students", teacherToken,
                new { registrationNo, fullName = "Smoke Test Student", email }, cancellationToken);
            CheckCreated("POST", "/api/students", createdStudent, "/api/students/");
            studentId = ReadInt(createdStudent.Body, "id");
            await CheckAsync(client, "POST", "/api/students", HttpStatusCode.Conflict, teacherToken,
                new { registrationNo, fullName = "Duplicate", email = $"other-{suffix}@example.test" }, cancellationToken);
            await CheckAsync(client, "PUT", $"/api/students/{studentId}", HttpStatusCode.OK, teacherToken,
                new { registrationNo, fullName = "Smoke Test Student Updated", email }, cancellationToken);

            var coursesResponse = await SendAsync(client, "GET", "/api/courses?page=1&pageSize=50", teacherToken, null, cancellationToken);
            CheckResponse("GET", "/api/courses?page=1&pageSize=50", HttpStatusCode.OK,
                coursesResponse.StatusCode, coursesResponse.Body);
            using (var document = JsonDocument.Parse(coursesResponse.Body))
            {
                var seededCourse = document.RootElement.GetProperty("items").EnumerateArray().FirstOrDefault();
                if (seededCourse.ValueKind != JsonValueKind.Undefined)
                {
                    var seededCourseId = seededCourse.GetProperty("id").GetInt32();
                    await CheckAsync(client, "GET", $"/api/courses/{seededCourseId}", HttpStatusCode.OK,
                        teacherToken, null, cancellationToken);
                    await CheckAsync(client, "GET", $"/api/courses/{seededCourseId}", HttpStatusCode.OK,
                        studentToken, null, cancellationToken);
                }
            }
            await CheckAsync(client, "GET", "/api/courses/2147483647", HttpStatusCode.NotFound, teacherToken, null, cancellationToken);
            var createCourse = await SendAsync(client, "POST", "/api/courses", teacherToken,
                new { code = courseCode, title = "Smoke Test Course", creditHours = 2 }, cancellationToken);
            CheckCreated("POST", "/api/courses", createCourse, "/api/courses/");
            courseId = ReadInt(createCourse.Body, "id");
            await CheckAsync(client, "PUT", $"/api/courses/{courseId}", HttpStatusCode.OK, teacherToken,
                new { code = courseCode, title = "Smoke Test Course Updated", creditHours = 3 }, cancellationToken);

            var enrollment = await SendAsync(client, "POST", $"/api/courses/{courseId}/enrollments",
                teacherToken, new { studentId }, cancellationToken);
            CheckCreated("POST", $"/api/courses/{courseId}/enrollments", enrollment, "/api/enrollments/");
            enrollmentId = ReadInt(enrollment.Body, "id");
            await CheckAsync(client, "POST", $"/api/courses/{courseId}/enrollments", HttpStatusCode.Conflict,
                teacherToken, new { studentId }, cancellationToken);
            await CheckAsync(client, "POST", "/api/courses/2147483647/enrollments", HttpStatusCode.NotFound,
                teacherToken, new { studentId }, cancellationToken);
            await CheckAsync(client, "GET", $"/api/courses/{courseId}/enrollments", HttpStatusCode.OK,
                teacherToken, null, cancellationToken);
            await CheckAsync(client, "GET", $"/api/courses/{courseId}/stats", HttpStatusCode.OK,
                teacherToken, null, cancellationToken);
            await CheckAsync(client, "GET", $"/api/courses/2147483647/stats", HttpStatusCode.NotFound,
                teacherToken, null, cancellationToken);
            await CheckAsync(client, "PUT", $"/api/enrollments/{enrollmentId}/mark", HttpStatusCode.BadRequest,
                teacherToken, new { mark = 101 }, cancellationToken);
            await CheckAsync(client, "PUT", $"/api/enrollments/{enrollmentId}/mark", HttpStatusCode.BadRequest,
                teacherToken, new { mark = -1 }, cancellationToken);
            await CheckAsync(client, "PUT", $"/api/enrollments/{enrollmentId}/mark", HttpStatusCode.OK,
                teacherToken, new { mark = 80 }, cancellationToken);
            await CheckAsync(client, "PUT", "/api/enrollments/2147483647/mark", HttpStatusCode.NotFound,
                teacherToken, new { mark = 80 }, cancellationToken);
            await CheckAsync(client, "DELETE", $"/api/students/{studentId}", HttpStatusCode.Conflict,
                teacherToken, null, cancellationToken);
            await CheckAsync(client, "DELETE", $"/api/courses/{courseId}", HttpStatusCode.Conflict,
                teacherToken, null, cancellationToken);
            await CheckAsync(client, "DELETE", $"/api/enrollments/{enrollmentId}", HttpStatusCode.NoContent,
                teacherToken, null, cancellationToken);
            enrollmentId = 0;
            await CheckAsync(client, "DELETE", $"/api/courses/{courseId}", HttpStatusCode.NoContent,
                teacherToken, null, cancellationToken);
            courseId = 0;
            await CheckAsync(client, "DELETE", $"/api/students/{studentId}", HttpStatusCode.NoContent,
                teacherToken, null, cancellationToken);
            studentId = 0;

            await CheckAsync(client, "DELETE", "/api/students/2147483647", HttpStatusCode.NotFound,
                teacherToken, null, cancellationToken);
            await CheckAsync(client, "DELETE", "/api/courses/2147483647", HttpStatusCode.NotFound,
                teacherToken, null, cancellationToken);
            await CheckAsync(client, "DELETE", "/api/enrollments/2147483647", HttpStatusCode.NotFound,
                teacherToken, null, cancellationToken);
        }
        finally
        {
            if (enrollmentId > 0)
                await CleanupAsync(client, "DELETE", $"/api/enrollments/{enrollmentId}", teacherToken, cancellationToken);
            if (courseId > 0)
                await CleanupAsync(client, "DELETE", $"/api/courses/{courseId}", teacherToken, cancellationToken);
            if (studentId > 0)
                await CleanupAsync(client, "DELETE", $"/api/students/{studentId}", teacherToken, cancellationToken);
        }

        Console.WriteLine(failures == 0
            ? "SMOKE TEST SUMMARY: PASS"
            : $"SMOKE TEST SUMMARY: FAIL ({failures} failed checks)");
        logger.LogInformation("Smoke test completed with {FailureCount} failed checks", failures);
        return failures == 0 ? 0 : 1;
    }

    private async Task<string?> LoginAsync(
        HttpClient client, string username, string password, CancellationToken cancellationToken)
    {
        var response = await SendAsync(client, "POST", "/api/auth/login", null,
            new { username, password }, cancellationToken);
        CheckResponse("POST", "/api/auth/login", HttpStatusCode.OK,
            response.StatusCode, response.Body);
        if (response.StatusCode != HttpStatusCode.OK)
            return null;
        using var document = JsonDocument.Parse(response.Body);
        return document.RootElement.GetProperty("token").GetString();
    }

    private async Task CheckAsync(
        HttpClient client, string method, string path, HttpStatusCode expected, string? token,
        object? body, CancellationToken cancellationToken)
    {
        var response = await SendAsync(client, method, path, token, body, cancellationToken);
        CheckResponse(method, path, expected, response.StatusCode, response.Body);
    }

    private void CheckResponse(
        string method, string path, HttpStatusCode expected, HttpStatusCode actual, string body)
    {
        var passed = actual == expected;
        if (!passed)
            failures++;
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")} | {method} {path} | expected {(int)expected} | actual {(int)actual}");
        if (!passed)
            logger.LogError("Smoke check failed for {Method} {Path}: expected {Expected}, received {Actual}. Body: {Body}",
                method, path, (int)expected, (int)actual, body);
    }

    private void CheckCreated(
        string method, string path, (HttpStatusCode StatusCode, string Body, string? Location) response,
        string expectedLocationPrefix)
    {
        CheckResponse(method, path, HttpStatusCode.Created, response.StatusCode, response.Body);
        var location = response.Location ?? "";
        var hasLocation = location.Contains(expectedLocationPrefix, StringComparison.OrdinalIgnoreCase);
        if (!hasLocation)
            failures++;
        Console.WriteLine($"{(hasLocation ? "PASS" : "FAIL")} | {method} {path} | expected Location beginning {expectedLocationPrefix} | actual {location}");
    }

    private async Task<(HttpStatusCode StatusCode, string Body, string? Location)> SendAsync(
        HttpClient client, string method, string path, string? token, object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken),
            response.Headers.Location?.ToString());
    }

    private async Task CleanupAsync(
        HttpClient client, string method, string path, string token, CancellationToken cancellationToken)
    {
        try
        {
            var response = await SendAsync(client, method, path, token, null, cancellationToken);
            if ((int)response.StatusCode is < 200 or >= 300)
            {
                failures++;
                logger.LogError("Smoke cleanup failed for {Method} {Path}: status {StatusCode}",
                    method, path, (int)response.StatusCode);
                Console.WriteLine($"FAIL | cleanup {method} {path} | expected 204 | actual {(int)response.StatusCode}");
            }
        }
        catch (HttpRequestException exception)
        {
            failures++;
            logger.LogError(exception, "Smoke cleanup request failed for {Method} {Path}", method, path);
            Console.WriteLine($"FAIL | cleanup {method} {path} | expected 204 | actual ERROR");
        }
    }

    private static int ReadInt(string json, string property)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty(property, out var value) && value.TryGetInt32(out var id)
            ? id
            : throw new InvalidOperationException($"Expected response property '{property}' was missing.");
    }
}
