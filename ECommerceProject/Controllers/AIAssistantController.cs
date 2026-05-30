using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AIAssistantController : ControllerBase
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<AIAssistantController> _logger;

    public AIAssistantController(IGeminiService geminiService, ILogger<AIAssistantController> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { success = false, message = "Question is required" });
        }

        try
        {
            var response = await _geminiService.GetProductAssistantResponseAsync(
                request.ProductName ?? "",
                request.ProductDescription ?? "",
                request.Question
            );

            return Ok(new { success = true, response });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Gemini API error");
            return StatusCode(429, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Assistant unexpected error");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
        }
    }
}

public class AskRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public string Question { get; set; } = string.Empty;
}
