using System.ComponentModel.DataAnnotations;

namespace Clarius.OpenLaw;

public class OpenAISettings
{
    [Required(ErrorMessage = "Agent name is required")]
    public required string Agent { get; set; }

    [Required(ErrorMessage = "Key is required")]
    public required string Key { get; set; }

    public string Model { get; set; } = "gpt-4.1-mini";

    /// <summary>
    /// Minimum semantic similarity score to consider a document relevant.
    /// </summary>
    public float Score { get; set; } = 0.5f;

    public string[] Stores { get; set; } = [];
}
