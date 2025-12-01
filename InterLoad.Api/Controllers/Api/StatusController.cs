using Microsoft.AspNetCore.Mvc;

namespace InterLoad.Controllers.Api;

[Route("/api/[controller]")]
public class StatusController : ApiControllerBase
{
    [HttpGet]
    public StatusDtoOut GetStatus()
    {
        return new StatusDtoOut
        {
            GitCommitHash = Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "Unknown"
        };
    }

    public class StatusDtoOut
    {
        public required string GitCommitHash { get; init; }
    }
}