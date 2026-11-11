Student Marks Management APIA production-grade RESTful Web API built with .NET 10 Minimal APIs, Entity Framework Core 10, and PostgreSQL for managing student records, course catalogs, enrollment records, and academic grades.Architecture & FeaturesFramework & Runtime: ASP.NET Core (.NET 10) Minimal APIs.Database & Persistence: PostgreSQL via Entity Framework Core 10 (Npgsql driver with automatic snake_case database naming conventions).Authentication & Security: JWT Bearer Authentication with secure password hashing (PasswordHasher<User>).Role-Based Authorization: Role-restricted access policies distinguishing Teacher operations (full CRUD, enrollments, marks entry) from Student operations (read-only grades).Error Handling: Global exception handling middleware implementing standard RFC 7807 Problem Details JSON responses.Diagnostics & Open API: Native .NET 10 OpenAPI spec (/openapi/v1.json) and database health monitoring (/health).Automated Bootstrapping: Automatic database migration application and demo account seeding on startup.PrerequisitesBefore running the application, ensure your environment has the following installed:.NET 10 SDKPostgreSQL Database Server (Running locally on port 5432)GitQuick Start (Clean Copy Setup)Follow these exact steps to clone, configure, run, and test the project from scratch.1. Clone the RepositoryBashgit clone https://github.com/sardarsameerkhan/Student-Marks-Management-API.git
cd Student-Marks-Management-API
2. Configure Database ConnectionOpen src/MarksApi/appsettings.Development.json and ensure your local PostgreSQL connection string and credentials match your local setup:JSON{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=student_marks_db;Username=postgres;Password=your_postgres_password"
  },
  "Jwt": {
    "Issuer": "MarksApi",
    "Audience": "MarksApiUsers",
    "Key": "YourSuperSecretKeyHereWhichIsAtLeast32BytesLong!"
  }
}
Note: Update Password=your_postgres_password with your local PostgreSQL postgres user password.3. Build & Run the APINavigate to the API project directory and launch the server:Bashcd src/MarksApi
dotnet restore
dotnet build
dotnet run
Upon launch, the application will automatically:Apply any pending database migrations to your PostgreSQL server.Create the student_marks_db database if it does not exist.Seed default demo accounts (teacher and student) and sample courses into PostgreSQL.Listen on http://localhost:5191 (or your local environment port printed in the terminal).Seeding & Default CredentialsOn application startup, the API populates default users with pre-hashed passwords:RoleUsernamePasswordDefault PermissionsTeacherteacherTeacher123!Create/Update/Delete Students, Courses, Enrollments, MarksStudentstudentStudent123!Read-only access to own enrollments and assigned marksAPI Endpoints Reference1. System & DiagnosticsMethodEndpointAuthorizationDescriptionGET/PublicRoot welcome messageGET/healthPublicEF Core PostgreSQL connection health checkGET/openapi/v1.jsonPublicNative .NET 10 OpenAPI specification JSON2. AuthenticationMethodEndpointAuthorizationDescriptionPOST/api/auth/loginPublicAuthenticates user and returns a signed JWT Token3. Student ManagementMethodEndpointAuthorizationDescriptionGET/api/studentsAuthorizedList all studentsGET/api/students/{id}AuthorizedGet student details by IDPOST/api/studentsTeacher OnlyCreate a new student recordPUT/api/students/{id}Teacher OnlyUpdate an existing studentDELETE/api/students/{id}Teacher OnlyDelete a studentGET/api/students/{id}/marksAuthorizedView all enrolled course marks for a student4. Course ManagementMethodEndpointAuthorizationDescriptionGET/api/coursesAuthorizedList all active coursesGET/api/courses/{id}AuthorizedGet course details by IDPOST/api/coursesTeacher OnlyCreate a new coursePUT/api/courses/{id}Teacher OnlyUpdate course detailsDELETE/api/courses/{id}Teacher OnlyRemove a course5. Enrollments & Marks RecordingMethodEndpointAuthorizationDescriptionPOST/api/courses/{id}/enrollmentsTeacher OnlyEnroll a student into a courseGET/api/courses/{id}/enrollmentsTeacher OnlyGet all student enrollments for a coursePUT/api/courses/{id}/students/{studentId}/marksTeacher OnlyAssign or update a student's mark for a courseStep-by-Step Testing Guide (Postman / cURL)Step 1: Login as TeacherSend a POST request to /api/auth/login:Bashcurl -X POST http://localhost:5191/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "teacher", "password": "Teacher123!"}'
Response: Returns a JSON payload containing the JWT token. Copy this token string.Step 2: Create a Student (Teacher Authorization Required)Include the JWT token as a Bearer token in the Authorization header:Bashcurl -X POST http://localhost:5191/api/students \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TEACHER_JWT_TOKEN>" \
  -d '{
    "registrationNo": "FA24-BSCS-099",
    "fullName": "Alice Smith",
    "email": "alice.smith@student.edu"
  }'
Response: 201 Created with created student details.Step 3: Assign Course Marks (Teacher Only)Bashcurl -X PUT http://localhost:5191/api/courses/1/students/1/marks \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TEACHER_JWT_TOKEN>" \
  -d '{"mark": 92.5}'
Response: 200 OK showing updated course mark.Step 4: Verify Role-Based Authorization EnforcementLogin as student to retrieve a Student JWT Token, then attempt a Teacher action:Bashcurl -X POST http://localhost:5191/api/courses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <STUDENT_JWT_TOKEN>" \
  -d '{"code": "CS999", "title": "Unauthorized Course", "creditHours": 3}'
