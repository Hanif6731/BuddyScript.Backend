using BuddyScript.Backend.Data;
using BuddyScript.Backend.DTOs;
using BuddyScript.Backend.Models;
using BuddyScript.Backend.Repositories;
using BuddyScript.Backend.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BuddyScript.Backend.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public AuthService(IUserRepository userRepository, IConfiguration configuration, ApplicationDbContext context)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _context = context;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
            {
                await tx.RollbackAsync();
                return null;
            }

            var user = new User
            {
                FirstName = InputSanitizer.Sanitize(dto.FirstName),
                LastName  = InputSanitizer.Sanitize(dto.LastName),
                Email     = dto.Email.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            await tx.CommitAsync();

            return new AuthResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName  = user.LastName,
                Email     = user.Email
            };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(AuthResponseDto? user, string? token)> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant());
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return (null, null);

        var token = GenerateJwtToken(user);
        var res = new AuthResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName  = user.LastName,
            Email     = user.Email
        };
        return (res, token);
    }

    public async Task<AuthResponseDto?> GetUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;
        return new AuthResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName  = user.LastName,
            Email     = user.Email
        };
    }

    public async Task<(AuthResponseDto user, string token)> ProcessGoogleUserAsync(
        string email, string firstName, string lastName)
    {
        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(normalizedEmail);

            if (user == null)
            {
                user = new User
                {
                    Email        = normalizedEmail,
                    FirstName    = InputSanitizer.Sanitize(firstName),
                    LastName     = InputSanitizer.Sanitize(lastName),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                };
                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();
            }

            await tx.CommitAsync();

            var token = GenerateJwtToken(user);
            var res = new AuthResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName  = user.LastName,
                Email     = user.Email
            };
            return (res, token);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer  = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(tokenDescriptor));
    }
}
