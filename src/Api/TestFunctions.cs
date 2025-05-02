using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Clarius.OpenLaw;

public class TestFunctions(ILogger<TestFunctions> logger)
{
    [Function("test")]
    public IActionResult Test([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test")] HttpRequest req)
    {
        logger.LogInformation("Received test message.");
        return new OkResult();
    }
}
