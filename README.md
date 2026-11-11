Student Marks Management API
A production-grade RESTful Web API built with .NET 10 Minimal APIs, Entity Framework Core 10, and PostgreSQL for managing student records, course catalogs, enrollment records, and academic grades.

Architecture & Features
Framework & Runtime: ASP.NET Core (.NET 10) Minimal APIs.

Database & Persistence: PostgreSQL via Entity Framework Core 10 (Npgsql driver with automatic snake_case database naming conventions).

Authentication & Security: JWT Bearer Authentication with secure password hashing (PasswordHasher<User>).

Role-Based Authorization: Role-restricted access policies distinguishing Teacher operations (full CRUD, enrollments, marks entry) from Student operations (read-only grades).

Error Handling: Global exception handling middleware implementing standard RFC 7807 Problem Details JSON responses.

Diagnostics & Open API: Native .NET 10 OpenAPI spec (/openapi/v1.json) and database health monitoring (/health).

Automated Bootstrapping: Automatic database migration application and demo account seeding on startup.

Prerequisites
Before running the application, ensure your environment has the following installed:

.NET 10 SDK

PostgreSQL Database Server (Running locally on port 5432)

Git
