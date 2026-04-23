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
public class InteractionsController : ControllerBase
{
    private readonly IInteractionsService _interactionsService;

    public InteractionsController(IInteractionsService interactionsService)
    {
        _interactionsService = interactionsService;
    }

    [HttpPost("comment")]
    [EnableRateLimiting("writes")]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        try {
            var commentId = await _interactionsService.CreateCommentAsync(userId, dto);
            return Ok(new { Id = commentId });
        } catch (UnauthorizedAccessException) {
            return Forbid();
        } catch (KeyNotFoundException ex) {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("comments/{postId}")]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetComments(int postId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (postId <= 0) return BadRequest();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        try {
            var comments = await _interactionsService.GetTopLevelCommentsAsync(postId, userId, page, pageSize);
            return Ok(comments);
        } catch (UnauthorizedAccessException) {
            return Forbid();
        } catch (KeyNotFoundException ex) {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("replies/{commentId}")]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetReplies(int commentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (commentId <= 0) return BadRequest();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        try {
            var replies = await _interactionsService.GetRepliesAsync(commentId, userId, page, pageSize);
            return Ok(replies);
        } catch (UnauthorizedAccessException) {
            return Forbid();
        } catch (KeyNotFoundException ex) {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("like")]
    [EnableRateLimiting("writes")]
    public async Task<IActionResult> ToggleLike([FromBody] LikeDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        try {
            var reaction = await _interactionsService.ToggleLikeAsync(userId, dto);
            return Ok(new { Liked = reaction != null, Reaction = reaction });
        } catch (UnauthorizedAccessException) {
            return Forbid();
        } catch (KeyNotFoundException ex) {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("likers/{entityId}/{entityType}")]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetLikers(int entityId, int entityType)
    {
        if (entityId <= 0 || entityType < 0 || entityType > 1) return BadRequest();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        try {
            var likers = await _interactionsService.GetLikersAsync(entityId, entityType, userId);
            return Ok(likers);
        } catch (UnauthorizedAccessException) {
            return Forbid();
        } catch (KeyNotFoundException ex) {
            return NotFound(new { message = ex.Message });
        }
    }
}
