using System.ComponentModel.DataAnnotations;

namespace Clarius.OpenLaw;

public class OpenAISettings
{
    [Required(ErrorMessage = "Key is required")]
    public required string Key { get; set; }
    [Required(ErrorMessage = "Agent name is required")]
    public required string Agent { get; set; }
}
