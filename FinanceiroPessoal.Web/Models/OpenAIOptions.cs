namespace FinanceiroPessoal.Web.Models;

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-5.5-mini";
    public bool Enabled { get; set; } = false;
}
