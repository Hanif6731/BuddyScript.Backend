using BuddyScript.Backend.DTOs;
using BuddyScript.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BuddyScript.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;

    public AuthController(IAuthService authService, IConfiguration config)
    {
        _authService = authService;
        _config      = config;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var res = await _authService.RegisterAsync(dto);
        if (res == null) return BadRequest(new { message = "Email is already registered." });
        return Ok(res);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var (user, token) = await _authService.LoginAsync(dto);
        if (user == null || token == null)
            return Unauthorized(new { message = "Invalid email or password." });

        var cookieName    = _config["Jwt:CookieName"] ?? "buddyscript_auth";
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.None,
            Expires  = dto.RememberMe
                ? DateTime.UtcNow.AddDays(30)
                : DateTime.UtcNow.AddHours(24)
        };
        Response.Cookies.Append(cookieName, token, cookieOptions);

        return Ok(user);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var cookieName = _config["Jwt:CookieName"] ?? "buddyscript_auth";
        Response.Cookies.Delete(cookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.None
        });
        return Ok();
    }

    [HttpPost("google-login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        try
        {
            var clientId     = _config["Google:ClientId"];
            var clientSecret = _config["Google:ClientSecret"];

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code",          dto.Code),
                new KeyValuePair<string, string>("client_id",     clientId!),
                new KeyValuePair<string, string>("client_secret", clientSecret!),
                new KeyValuePair<string, string>("redirect_uri",  "postmessage"),
                new KeyValuePair<string, string>("grant_type",    "authorization_code")
            });

            var response = await client.PostAsync("https://oauth2.googleapis.com/token", content);
            var json     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return BadRequest(new { message = "Google authentication failed." });

            var tokenData = System.Text.Json.JsonDocument.Parse(json);
            var idToken   = tokenData.RootElement.GetProperty("id_token").GetString();

            var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };
            var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            var (user, jwtToken) = await _authService.ProcessGoogleUserAsync(
                payload.Email,
                payload.GivenName  ?? "User",
                payload.FamilyName ?? "");

            var cookieName    = _config["Jwt:CookieName"] ?? "buddyscript_auth";
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.None,
                Expires  = DateTime.UtcNow.AddHours(24)
            };
            Response.Cookies.Append(cookieName, jwtToken, cookieOptions);

            return Ok(user);
        }
        catch
        {
            return BadRequest(new { message = "Google authentication failed." });
        }
    }

    public class GoogleLoginDto
    {
        public string Code { get; set; } = string.Empty;
    }

    [HttpGet("getcurrentuser")]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var user = await _authService.GetUserAsync(userId);
        if (user == null) return Unauthorized();

        return Ok(user);
    }
}
