using BuddyScript.Backend.DTOs;

namespace BuddyScript.Backend.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
    Task<(AuthResponseDto? user, string? token)> LoginAsync(LoginDto dto);
    Task<AuthResponseDto?> GetUserAsync(int userId);
    Task<(AuthResponseDto user, string token)> ProcessGoogleUserAsync(string email, string firstName, string lastName);
}
