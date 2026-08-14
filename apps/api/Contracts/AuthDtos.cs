namespace Api.Contracts;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record UserDto(Guid Id, string Email);

public record AuthResponse(string AccessToken, UserDto User);
