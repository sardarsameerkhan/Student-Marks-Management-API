using Microsoft.EntityFrameworkCore;
using MarksApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database"))
           .UseSnakeCaseNamingConvention());

var app = builder.Build();

app.MapGet("/", () => "Student Marks Management API is running!");

app.Run();