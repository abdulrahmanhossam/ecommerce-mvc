using Microsoft.AspNetCore.Mvc;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Controllers;

[Route("api/[controller]")]
[ApiController]
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
            _logger.LogInformation("AI Assistant request for product: {ProductName}", request.ProductName);

            var response = await _geminiService.GetProductAssistantResponseAsync(
                request.ProductName,
                request.ProductDescription ?? "",
                request.Question
            );

            return Ok(new { success = true, response });
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("AI Assistant timeout for product: {ProductName}", request.ProductName);
            return StatusCode(504, new { success = false, message = "Request timed out. Please try again." });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI Assistant API error");
            return StatusCode(502, new { success = false, message = "AI service temporarily unavailable. Please try again later." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Assistant unexpected error");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred. Please try again." });
        }
    }
}

public class AskRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public string Question { get; set; } = string.Empty;
}