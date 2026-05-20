namespace ECommerceProject.Services.Interfaces;

public interface IGeminiService
{
    Task<string> GetProductAssistantResponseAsync(string productName, string productDescription, string userQuestion);
}