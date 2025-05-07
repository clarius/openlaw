using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OpenAI;
using static Xunit.SecretsFactAttribute;

namespace Clarius.OpenLaw;

public class ComplexityTests(ITestOutputHelper output)
{
    [InlineData("¿Cómo maneja el Código Procesal Civil y Comercial las situaciones de rebeldía y qué impacto tiene esto en el resultado del proceso judicial?", ComplexityLevel.Medium)]
    [InlineData("Cuál es el título completo, número y fecha de sanción de la Ley Bases?", ComplexityLevel.Low)]
    [InlineData("¿Cuáles son los requisitos para la validez de un contrato de trabajo en Argentina?", ComplexityLevel.Medium)]
    [LocalTheory("Gemini:Key", "Gemini:Model")]
    public async Task CanEvaluateComplexity(string question, ComplexityLevel expectedComplexity)
    {
        var client = new OpenAIClient(new ApiKeyCredential(Configuration.Get("Gemini:Key")), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/"),
        })
            .GetChatClient(Configuration.Get("Gemini:Model"))
            .AsIChatClient()
            .AsBuilder()
            .UseLogging(output.AsLoggerFactory())
            .Build();

        var complexity = new ComplexityAssessment(client);
        var result = await complexity.EvaluateAsync(question);
        Assert.Equal(expectedComplexity, result);
    }
}
