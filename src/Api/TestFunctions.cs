using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Clarius.OpenLaw;

public class TestFunctions(VectorStoreService vectors, ILogger<TestFunctions> logger)
{
    [Function("vectors")]
    public async Task<IActionResult> VectorsAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vectors")] HttpRequest req)
    {
        return new OkObjectResult(await vectors.GetStores());
    }
}
