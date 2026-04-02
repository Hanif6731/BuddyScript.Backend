using BuddyScript.Backend.Data;
using BuddyScript.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;

namespace BuddyScript.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetSettings()
    {
        var userId   = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new UserSettings { UserId = userId, Theme = "light" };
                _context.UserSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return Ok(new { theme = settings.Theme });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpPatch("theme")]
    [EnableRateLimiting("writes")]
    public async Task<IActionResult> UpdateTheme([FromBody] ThemeUpdateDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new UserSettings { UserId = userId, Theme = dto.Theme };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.Theme = dto.Theme;
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { theme = settings.Theme });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}

public class ThemeUpdateDto
{
    [Required]
    [RegularExpression("^(light|dark)$", ErrorMessage = "Theme must be 'light' or 'dark'.")]
    public string Theme { get; set; } = "light";
}
