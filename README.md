# Student Marks Management API

ASP.NET Core 8 minimal API, Entity Framework Core 8, and PostgreSQL. Endpoints are grouped into small service interfaces, use DTOs, async EF calls, JWT bearer authentication, role authorization, validation filters, standard Problem Details errors, paging, and read-only projections.

## Requirements

- .NET 8 SDK
- PostgreSQL 16 or newer
- `dotnet-ef` tool
- Postman (for the exported collection)

## Local setup

Run commands in PowerShell from the repository folder.

1. Create a PostgreSQL role and database, or start a local PostgreSQL container:

   ```powershell
   docker run -d --name marks-postgres -e POSTGRES_USER=marks_user -e POSTGRES_PASSWORD=choose_a_local_password -e POSTGRES_DB=marks_db -p 5432:5432 postgres:17
   ```

2. Move into the API project, initialize user-secrets, and set local-only secrets. Choose your own passwords and JWT key; the key must be at least 32 bytes.

   ```powershell
   cd .\src\MarksApi
   dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=marks_db;Username=marks_user;Password=choose_a_local_password"
   dotnet user-secrets set "Jwt:Key" "replace-with-a-random-secret-at-least-32-bytes-long"
   dotnet user-secrets set "Seed:TeacherPassword" "choose-a-teacher-demo-password"
   dotnet user-secrets set "Seed:StudentPassword" "choose-a-student-demo-password"
   ```

   The seeded accounts are `teacher1` (Teacher), and `student1` and `student2` (Students). Both Student accounts use the configured `Seed:StudentPassword`; the Teacher uses `Seed:TeacherPassword`. These are development-only credentials; use non-production values and do not put them in source control.

3. Install EF tools if needed, create/update the database from the committed migration, and run the API:

   ```powershell
   dotnet tool install --global dotnet-ef
   dotnet ef database update
   dotnet run
   ```

   Seeding runs at startup after the migration has created the tables. The seed includes two students, three courses, six enrollments, and three hashed-password accounts. Set `PassMark` and `Cors:Origins` in configuration as required. The fake-value template is [appsettings.Example.json](./src/MarksApi/appsettings.Example.json).

4. Open `/swagger` on the printed API address. `GET /health` checks PostgreSQL connectivity and is anonymous.

For Docker-based PostgreSQL, the app still runs locally with the same connection string. Configure the database and JWT key as environment variables in a deployed environment (`ConnectionStrings__Database`, `Jwt__Key`, and so on).

## Routes

| Method | Path | Access |
|---|---|---|
| POST | `/api/auth/login` | Anonymous |
| GET, POST | `/api/students` | Teacher |
| GET, PUT, DELETE | `/api/students/{id}` | Teacher; GET also allows the owning Student |
| GET | `/api/students/{id}/transcript` | Teacher; owning Student |
| GET, POST | `/api/courses` | Teacher; GET also allows Student |
| GET, PUT, DELETE | `/api/courses/{id}` | Teacher; GET also allows Student |
| POST, GET | `/api/courses/{courseId}/enrollments` | Teacher |
| GET | `/api/courses/{courseId}/stats` | Teacher |
| PUT, DELETE | `/api/enrollments/{id}/mark`, `/api/enrollments/{id}` | Teacher |
| GET | `/health` | Anonymous |

List responses use `items`, `page`, `pageSize`, and `totalCount`. Search and sort are available on students, courses, and course enrollments; supported sort names include `fullName`, `registrationNo`, `email`, `code`, `title`, `creditHours`, `studentId`, and `mark`. Prefix a sort name with `-` for descending order. Page size is capped at 50.

## Smoke test

The smoke runner starts the API on a loopback ephemeral port, calls the real HTTP endpoints against PostgreSQL, prints expected/actual status per check, and removes its uniquely prefixed test records. It reads login passwords from the same user-secrets/environment configuration as the API.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run -- --smoke-test
```

It exits with code 0 when all checks pass and a non-zero code otherwise. It is never started implicitly in Production.

## Postman

Import `postman/MarksApi.postman_collection.json` and `postman/Local.postman_environment.json`. Set `baseUrl` to the address shown by `dotnet run`, and set the environment's `teacherPassword` and `studentPassword` to the same values configured in user-secrets. The collection saves the teacher login token automatically; student-specific checks use a separate collection variable.

## Data model and design notes

The EF migration is in `src/MarksApi/Migrations`; `db/schema.sql` is a readable schema reference. Unique database indexes arbitrate concurrent duplicate creates and enrollments; PostgreSQL unique violations are returned as 409. Foreign keys restrict deletion when relationships exist. Marks are nullable until graded and checked both by DTO validation and PostgreSQL.

The course average excludes ungraded enrollments. Transcript averages use `sum(mark × creditHours) / sum(creditHours)` across graded courses. Pass mark is read from configuration (default 50). All timestamps are assigned in UTC.

## Verification deliverables

- [docs/report.pdf](./docs/report.pdf): four-page project and verification report.
- [docs/erd.png](./docs/erd.png): ER diagram of the four database tables.
- [docs/VERIFICATION.md](./docs/VERIFICATION.md): test environment, exact results, and reproducible commands.
- [docs/smoke-test-output.txt](./docs/smoke-test-output.txt): actual smoke-test run output (44 checks).
- [docs/newman-test-output.txt](./docs/newman-test-output.txt): actual Newman run output (43 requests, 116 assertions).
