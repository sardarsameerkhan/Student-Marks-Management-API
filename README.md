# Student Marks Management API

A RESTful Web API built with **.NET 10** and **PostgreSQL** for managing students, courses, enrollments, and academic marks. Designed using Minimal APIs, Entity Framework Core, JWT Authentication, and RFC 7807 Problem Details for global error handling.

## Features

- **JWT Authentication & Authorization**: Role-based access control (`Teacher` vs. `Student`).
- **Student Management**: CRUD operations for student records (Teacher restricted for modifications).
- **Course Management**: Create, update, list, and delete courses.
- **Enrollment & Marks Recording**: Assign students to courses and record course marks.
- **Database Migrations & Seeding**: Automated schema updates and initial demo user/course data.
- **Health Checks & OpenAPI**: `/health` endpoint for database monitoring and `/openapi/v1.json` spec.

## Tech Stack

- **Framework**: ASP.NET Core (.NET 10) Minimal APIs
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 10 (Npgsql Provider)
- **Authentication**: JWT Bearer Tokens
- **Naming Convention**: Snake Case (Database Column Mapping)

## Getting Started

### Prerequisites
Endpoints Summary
Auth: POST /api/auth/login

Students: GET /api/students, POST /api/students, GET /api/students/{id}, PUT /api/students/{id}, DELETE /api/students/{id}, GET /api/students/{id}/marks

Courses: GET /api/courses, POST /api/courses, GET /api/courses/{id}, PUT /api/courses/{id}, DELETE /api/courses/{id}

Enrollments & Marks: POST /api/courses/{id}/enrollments, GET /api/courses/{id}/enrollments, PUT /api/courses/{id}/students/{studentId}/marks

Diagnostics: GET /health, GET /openapi/v1.json

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [PostgreSQL](https://www.postgresql.org/) running locally
- EF Core CLI Tool (`dotnet tool install --global dotnet-ef --version 10.0.0`)

### Configuration

Update your database connection string and JWT settings in `src/MarksApi/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=student_marks_db;Username=postgres;Password=your_password"
  },
  "Jwt": {
    "Issuer": "MarksApi",
    "Audience": "MarksApiUsers",
    "Key": "YourSuperSecretKeyHereWhichIsAtLeast32BytesLong!"
  }
}
