using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Clarius.OpenLaw;

//[Service(ServiceLifetime.Transient)]
public class DefineCommand(IVectorStoreService stores, IOptions<OpenAISettings> settings) : VectorSearchCommand(stores, settings)
{
    [Description("Search the definition of an acronym or technical term in the current legal corpus.")]
    public override Task<string> Execute([Description("Acronym or technical term to look up.")] string term) => base.Execute(term);
}
