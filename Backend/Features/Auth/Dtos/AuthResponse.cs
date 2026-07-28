namespace Backend.Features.Auth.Dtos;

public record AuthResponse(string AccessToken, UserDto User);