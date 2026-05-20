using System.Text;
using System.Text.Json;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Services;

public class GeminiService : IGeminiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly double _temperature;

    public GeminiService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        
        _apiKey = _configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API key not configured");
        _model = _configuration["Gemini:Model"] ?? "gemini-2.0-flash";
        _maxTokens = int.Parse(_configuration["Gemini:MaxTokens"] ?? "500");
        _temperature = double.Parse(_configuration["Gemini:Temperature"] ?? "0.7");
    }

    public async Task<string> GetProductAssistantResponseAsync(string productName, string productDescription, string userQuestion)
    {
        try
        {
            var prompt = BuildPrompt(productName, productDescription, userQuestion);
            var requestBody = BuildRequestBody(prompt);

            var client = _httpClientFactory.CreateClient("Gemini");
            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-goog-api-key", _apiKey);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling Gemini API: {Url}", url);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API {StatusCode}. Body: {Body}", response.StatusCode, errorContent);

                if ((int)response.StatusCode == 429)
                {
                    return "⚠️ AI is temporarily overloaded. Please wait a moment and try again.";
                }

                throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return ExtractTextFromResponse(responseContent);
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Gemini API request timed out");
            throw new TimeoutException("The AI request timed out. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Gemini API request failed");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Gemini service");
            throw;
        }
    }

    private string BuildPrompt(string productName, string productDescription, string userQuestion)
    {
        return $@"You are a helpful product assistant for an e-commerce store. 
Product Name: {productName}
Product Description: {productDescription}

Customer Question: {userQuestion}

Provide a helpful, concise, and friendly response about this product. If the question is not related to the product, politely redirect to product-related topics.";
    }

    private string BuildRequestBody(string prompt)
    {
        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 800,
                temperature = _temperature
            }
        };

        return JsonSerializer.Serialize(request, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
    }

    private string ExtractTextFromResponse(string jsonResponse)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonResponse);
            var root = document.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) && 
                candidates.ValueKind == JsonValueKind.Array && 
                candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                
                if (firstCandidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.ValueKind == JsonValueKind.Array &&
                    parts.GetArrayLength() > 0)
                {
                    var fullText = new StringBuilder();

                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var textElement))
                        {
                            var text = textElement.GetString();
                            if (!string.IsNullOrEmpty(text))
                            {
                                fullText.Append(text);
                            }
                        }
                    }

                    if (fullText.Length > 0)
                    {
                        return fullText.ToString();
                    }
                }
            }

            if (root.TryGetProperty("promptFeedback", out var feedback))
            {
                _logger.LogWarning("Gemini prompt was blocked: {Feedback}", feedback.GetRawText());
                return "I'm sorry, I couldn't process that request. Please try a different question.";
            }

            _logger.LogWarning("Unexpected Gemini response format: {Response}", jsonResponse);
            return "I'm sorry, I couldn't process that request.";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini response");
            return "I'm sorry, there was an error processing the response.";
        }
    }
}