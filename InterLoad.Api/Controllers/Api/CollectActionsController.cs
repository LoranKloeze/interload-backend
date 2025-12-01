using InterLoad.Data;
using InterLoad.Dtos;
using InterLoad.Models.Ef;
using InterLoad.Utils;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Controllers.Api;

[Route("/api/[controller]")]
public class CollectActionsController(ApplicationDbContext context): ApiControllerBase
{

    [HttpGet]
    public async Task<Ok<List<CollectActionDtoOut>>> GetAll(int page, int pageSize = 25)
    {
        var dtos = await context.CollectActions.AsNoTracking()
            .OrderByDescending(ca => ca.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ca => new CollectActionDtoOut
            {
                Id = ca.Id,
                CreatedAt = ca.CreatedAt,
                UserName = ca.KeycloakUserName,
                ProjectName = ca.CollectDemands.First().SubProject.Project.Name,
                SubProjectName = ca.CollectDemands.First().SubProject.Name,
                CollectableName = ca.CollectDemands.First().Collectable != null ? ca.CollectDemands.First().Collectable!.Name : ca.CollectDemands.First().Remark,
                Collected = ca.Collected
            }).ToListAsync();
        
        return TypedResults.Ok(dtos);
    }

    [HttpPost]
    public async Task<Ok> CreateCollectAction(CollectActionDtoIn dtoIn)
    {
        if (dtoIn.CollectDemandIds.Count == 0)
        {
            throw new InvalidOperationException("At least one CollectDemandId must be provided.");
        }
        
        var collectDemands = await context.CollectDemands
            .Where(cd => dtoIn.CollectDemandIds.Contains(cd.Id))
            .ToListAsync();

        var user = ExtractKeycloakUserFromHttpContext.Extract(HttpContext.User);
        var collectAction = new CollectAction
        {
            CollectDemands = collectDemands,
            Collected = dtoIn.Collected,
            KeycloakUserId = user?.UserId ?? Guid.Empty,
            KeycloakUserName = user?.UserName ?? "[anonymous]",
        };
        
        context.CollectActions.Add(collectAction);
        await context.SaveChangesAsync();

        return TypedResults.Ok();
    }
    
}