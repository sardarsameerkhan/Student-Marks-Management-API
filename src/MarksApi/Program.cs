using System.Security.Claims;
using System.Text;
using MarksApi;
using MarksApi.Data;
using MarksApi.Infrastructure;
using MarksApi.SmokeTests;
using MarksApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
var smokeRequested = args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase)
    || builder.Configuration.GetValue<bool>("SMOKE_TEST");
if (smokeRequested)
    builder.WebHost.UseUrls("http://127.0.0.1:0");

var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("ConnectionStrings:Database is not configured.");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Student Marks Management API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    options.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
if (smokeRequested)
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Smoke tests are disabled outside the Development environment.");
}
app.UseExceptionHandler();
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    var (title, detail) = response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized =>
            ("Unauthorized", "Authentication is required to access this resource."),
        StatusCodes.Status403Forbidden =>
            ("Forbidden", "You do not have permission to access this resource."),
        StatusCodes.Status404NotFound =>
            ("Not Found", "The requested resource was not found."),
        _ => ("Request Failed", "The request could not be completed.")
    };
    response.ContentType = "application/problem+json";
    await response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = response.StatusCode,
        Title = title,
        Detail = detail,
        Instance = statusContext.HttpContext.Request.Path
    }, statusContext.HttpContext.RequestAborted);
});
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapApiEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(
        db,
        scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>(),
        app.Configuration,
        CancellationToken.None);
}

if (smokeRequested)
{
    await app.StartAsync();
    int result;
    try
    {
        result = await new SmokeRunner(
            app.Configuration,
            app.Services.GetRequiredService<ILogger<SmokeRunner>>())
            .RunAsync(app.Services, CancellationToken.None);
    }
    finally
    {
        await app.StopAsync();
    }
    Environment.ExitCode = result;
    return;
}

await app.RunAsync();

public partial class Program { }
