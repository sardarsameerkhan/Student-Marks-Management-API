# Verification record

This record summarizes the environment, commands, and test results for the project.

## Environment

- .NET SDK: 8.0.428, run in the official Microsoft SDK container because the host SDK is not installed
- PostgreSQL: isolated, disposable PostgreSQL 16 container
- Postman collection runner: Newman, Node.js 22 Alpine container
- Database: created from the committed EF Core migration; seeded accounts and sample rows loaded by the application

No credentials were written to the project. Test-only values were supplied as temporary container environment variables.

## Results

| Check | Result |
|---|---:|
| `dotnet build` | PASS — 0 warnings, 0 errors |
| `dotnet ef database update` | PASS — initial migration applied |
| API smoke test, first run | PASS — 44 checks, 0 failures |
| API smoke test, repeated run | PASS — 44 checks, 0 failures |
| Newman collection | PASS — 43 requests, 43 scripts, 116 assertions, 0 failures |
| Seed/cleanup integrity | PASS — 2 students, 3 courses, 6 enrollments, 3 users remain after runs |
| Collection/environment JSON parse | PASS |

Smoke tests covered authentication, health, student/course CRUD, enrollment, mark updates, transcript and stats, creation `Location` headers, validation, authorization, duplicate conflicts, unknown IDs, and deletion conflicts. The repeated run used the same database and left the seed row counts unchanged.

Raw console records from the final runs are included as [smoke-test-output.txt](./smoke-test-output.txt) and [newman-test-output.txt](./newman-test-output.txt). The former includes the per-check expected and actual status codes; the latter includes each request and assertion result.

## Reproduce

From `src/MarksApi`, after configuring the local secrets in the main README:

```powershell
dotnet build
dotnet ef database update
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run -- --smoke-test
```

In another terminal, launch the API normally and run the imported Postman collection. For a command-line run, Newman is optional:

```powershell
npx --yes newman run .\postman\MarksApi.postman_collection.json `
  -e .\postman\Local.postman_environment.json `
  --env-var "baseUrl=http://localhost:5000" `
  --env-var "teacherPassword=<your configured teacher demo password>" `
  --env-var "studentPassword=<your configured student demo password>"
```
