using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Clarius.OpenLaw;

public enum ComplexityLevel { Low, Medium, High }

[Service]
public partial class ComplexityAssessment([FromKeyedServices("complexity")] IChatClient client)
{
    const string SystemPrompt =
        """
        You are an AI assistant designed to analyze user questions about legal matters. 
        Your task is to evaluate each question to determine its complexity, without 
        providing answers or accessing the legal corpus. The questions will ultimately 
        be answered by a Retrieval-Augmented Generation (RAG) system querying a country's 
        vast legal corpus. Your goal is to quickly assess whether a question can be 
        handled by a smaller model for quick fact lookup or requires a top-tier model 
        for deeper analysis or falls somewhere in between.

        # Instructions:
        ## Complexity Assessment:
        * Low Complexity: The question seeks a specific fact, a clear definition, or 
          a straightforward procedure. These are narrow in scope and typically involve 
          a single, well-defined legal concept. Examples: "What is the statute of 
          limitations for theft in Texas?" or "What does 'habeas corpus' mean?"
        * Medium Complexity: The question requires some interpretation, comparison, 
          or explanation of legal concepts or procedures. It may involve multiple 
          related facts or a moderately broad topic. Examples: "What are the steps to 
          file for divorce in Florida?" or "How do misdemeanor and felony charges differ?"
        * High Complexity: The question is broad, subtle, or involves multiple areas 
          of law, deep analysis, or emerging/novel issues. These often require 
          synthesizing complex legal principles or addressing ambiguity. Examples: 
          "How do privacy laws apply to drone surveillance?" or "What are the legal 
          implications of autonomous vehicles in contract law?"
        
        ## Output Format:
        Respond only with a JSON object in this exact structure:
        ```json
        {
          "complexity": "low|medium|high",
        }
        ```
        Do not include additional text, explanations, the actual answer to the question 
        or even attempt to answer it.

        # Guidelines:
        Base your assessment on your internal, generic knowledge of law and the nature 
        of legal questions, without accessing any external legal corpus.
        Focus on the question's phrasing, specificity, and the breadth or subtlety of 
        the legal topic in general terms.
        For complexity, consider how broad or subtle the question is in legal 
        terms—narrow facts are low, moderately broad topics are medium, and wide-ranging 
        or nuanced issues are high.
        """;

    public async Task<ComplexityLevel> EvaluateAsync(string content, CancellationToken cancellation = default)
    {
        var response = await client.GetResponseAsync<Assessment>(
            [
                new ChatMessage(ChatRole.System, SystemPrompt),
                new ChatMessage(ChatRole.User, content)
            ], AssessmentSerializerContext.Default.Assessment.Options, cancellationToken: cancellation);

        return response.Result.Complexity;
    }

    public record Assessment(ComplexityLevel Complexity);

    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UseStringEnumConverter = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip)]
    [JsonSerializable(typeof(Assessment))]
    partial class AssessmentSerializerContext : JsonSerializerContext { }
}