using System.Threading.Tasks;

public interface IAiService
{
    Task<string> GenerateContentAsync(string prompt, double? temperature = null, int? maxTokens = null);
    Task<object> GenerateContentWithDetailsAsync(string prompt, double? temperature = null, int? maxTokens = null);
}
