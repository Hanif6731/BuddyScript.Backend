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

        var commentId = await _interactionsService.CreateCommentAsync(userId, dto);
        return Ok(new { Id = commentId });
    }

    [HttpGet("comments/{postId}")]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetComments(int postId)
    {
        if (postId <= 0) return BadRequest();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var comments = await _interactionsService.GetTopLevelCommentsAsync(postId, userId);
        return Ok(comments);
    }

    [HttpGet("replies/{commentId}")]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetReplies(int commentId)
    {
        if (commentId <= 0) return BadRequest();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var replies = await _interactionsService.GetRepliesAsync(commentId, userId);
        return Ok(replies);
    }

    [HttpPost("like")]
    [EnableRateLimiting("writes")]
    public async Task<IActionResult> ToggleLike([FromBody] LikeDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var isLiked = await _interactionsService.ToggleLikeAsync(userId, dto);
        return Ok(new { Liked = isLiked });
    }

    [HttpGet("likers/{entityId}/{entityType}")]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetLikers(int entityId, int entityType)
    {
        if (entityId <= 0 || entityType < 0 || entityType > 1) return BadRequest();

        var likers = await _interactionsService.GetLikersAsync(entityId, entityType);
        return Ok(likers);
    }
}
