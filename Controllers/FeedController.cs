using BuddyScript.Backend.DTOs;
using BuddyScript.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BuddyScript.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FeedController : ControllerBase
{
    private readonly IFeedService _feedService;

    public FeedController(IFeedService feedService)
    {
        _feedService = feedService;
    }

    [HttpPost("posts")]
    [EnableRateLimiting("writes")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> CreatePost([FromForm] PostCreateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var postId = await _feedService.CreatePostAsync(userId, dto);
        return Ok(new { Id = postId });
    }

    [HttpGet("posts")]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var posts = await _feedService.GetFeedAsync(userId, page, pageSize);
        return Ok(posts);
    }

    [HttpGet("image/{postId}")]
    [AllowAnonymous]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetImage(int postId)
    {
        if (postId <= 0) return BadRequest();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = int.TryParse(userIdStr, out var id) ? id : null;

        try {
            var (data, mimeType) = await _feedService.GetPostImageAsync(postId, userId);
            if (data == null || mimeType == null) return NotFound();
            return File(data, mimeType);
        } catch (UnauthorizedAccessException) {
            return Forbid();
        } catch (KeyNotFoundException) {
            return NotFound();
        }
    }
}
