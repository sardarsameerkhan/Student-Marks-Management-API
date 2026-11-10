namespace MarksApi.Dtos;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, DateTime ExpiresAtUtc, string Role);