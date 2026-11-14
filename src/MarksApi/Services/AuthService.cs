using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MarksApi.Data;
using MarksApi.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MarksApi.Services;

public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    IConfiguration configuration,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == username, cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Failed login attempt for username {Username}", username);
            throw new ApiUnauthorizedException("The username or password is incorrect.");
        }

        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var expiresAt = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:ExpiresMinutes", 60));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };
        if (user.StudentId is int studentId)
            claims.Add(new Claim("studentId", studentId.ToString()));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"],
            configuration["Jwt:Audience"],
            claims,
            expires: expiresAt,
            signingCredentials: credentials);
        logger.LogInformation("User {Username} logged in with role {Role}", user.Username, user.Role);
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, user.Role);
    }
}
