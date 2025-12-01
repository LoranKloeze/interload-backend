using InterLoad.Data;
using InterLoad.Dtos;
using InterLoad.Models.Ef;
using InterLoad.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Controllers.Api;

[Route("/api/[controller]")]
public class CollectOrdersController(CollectOrderService collectOrderService, ApplicationDbContext context)
    : ApiControllerBase
{
    [HttpGet("current")]
    public async Task<Ok<List<CollectOrderDtoOut>>> GetCurrentCollectGroups()
    {
        var oneWeekAgo = DateTime.UtcNow.AddDays(-8);
        var fourWeeksAhead = DateTime.UtcNow.AddDays(32);

        var dtos = await collectOrderService.GetCurrent(oneWeekAgo, fourWeeksAhead);

        return TypedResults.Ok(dtos);
    }

    [HttpGet]
    public async Task<Ok<List<CollectOrderEntryDtoOut>>> GetOne(int projectId, int groupNr, bool prep)
    {
        var justUpdatedWindow = DateTime.UtcNow.AddSeconds(-5);
        var projectExists = await context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId);

        if (!projectExists)
        {
            throw new InvalidOperationException($"Project with ID {projectId} not found.");
        }

        var activeCollectGroupId = await context.CollectGroups
            .AsNoTracking()
            .Where(cg => cg.ProjectId == projectId && cg.IsActive)
            .Select(cg => (int?)cg.Id)
            .SingleOrDefaultAsync();

        if (activeCollectGroupId is null)
        {
            throw new InvalidOperationException(
                $"Active CollectGroup for Project ID {projectId} not found.");
        }

        var demands = await context.CollectDemands
            .Where(cd => context.CollectGroupEntries
                .Any(cge =>
                    cge.CollectGroupId == activeCollectGroupId &&
                    cge.GroupNr == groupNr &&
                    (!prep && cge.SubProjectId == cd.SubProjectId || prep && cge.SubProjectId == cd.SubProject.PrepSubProjectFor!.Id)
                ))
            .Select(cge => new CollectOrderEntryDtoOut
            {
                CollectDemandIds = new List<int> { cge.Id },
                Demand = cge.Demand,
                Collected = cge.CollectActions.Sum(ca => ca.Collected),
                Location = cge.Collectable != null ? cge.Collectable.Location : "-SPEC-",
                Description = cge.Collectable != null ? cge.Collectable.Name : cge.Remark,
                Remark = cge.Remark,
                Weight = cge.Collectable != null ? cge.Collectable.Weight : 0.00m,
                JustUpdated =
                    cge.UpdatedAt >= justUpdatedWindow ||
                    cge.CreatedAt >= justUpdatedWindow ||
                    cge.CollectActions.Any(ca => ca.CreatedAt >= justUpdatedWindow)
            })
            .ToListAsync();

        var withRemark = demands
            .Where(d => !string.IsNullOrWhiteSpace(d.Remark))
            .ToList();

        var groupedWithoutRemark = demands
            .Where(d => string.IsNullOrWhiteSpace(d.Remark))
            .GroupBy(d => new { d.Location, d.Description, d.Collected })
            .Select(g => new CollectOrderEntryDtoOut
            {
                CollectDemandIds = g.SelectMany(x => x.CollectDemandIds).OrderBy(id => id).ToList(),
                Demand = g.Sum(x => x.Demand),
                Location = g.Key.Location,
                Collected = g.Key.Collected,
                Description = g.Key.Description,
                Remark = string.Empty,
                Weight = g.Sum(x => x.Weight),
                JustUpdated = g.Any(x => x.JustUpdated)
            })
            .ToList();

        var finalDemands = withRemark
            .Concat(groupedWithoutRemark)
            .OrderBy(d => d.Location)
            .ThenBy(d => d.Description)
            .ThenBy(d => d.Remark)
            .ToList();
        return TypedResults.Ok(finalDemands);
    }

    [HttpGet("demand-details")]
    public async Task<Ok<CollectDemandDetailsDtoOut>> GetDemandDetails(string collectDemandIds)
    {
        var ids = collectDemandIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToList();

        var collectDemands = await context.CollectDemands.AsNoTracking()
            .Where(cd => ids.Contains(cd.Id))
            .Include(collectDemand => collectDemand.Collectable)
            .ToListAsync();

        ValidateCollectDemands(collectDemands);
        var firstCollectDemand = collectDemands.First();
        var collectable = firstCollectDemand.Collectable;

        var totalCollected = await context.CollectActions.AsNoTracking()
            .Where(ca => ca.CollectDemands.Select(cd => cd.Id).Any(id => ids.Contains(id)))
            .SumAsync(ca => ca.Collected);

        var totalDemand = collectDemands.Sum(cd => cd.Demand);
        return TypedResults.Ok(new CollectDemandDetailsDtoOut
        {
            TotalDemand = totalDemand,
            TotalCollected = totalCollected,
            Description = collectable?.Name ?? collectDemands.First().Remark,
            Remark = firstCollectDemand.Remark
        });
    }

    private static void ValidateCollectDemands(List<CollectDemand> collectDemands)
    {
        if (collectDemands.Count == 0)
        {
            throw new InvalidOperationException("No CollectDemands found for the provided IDs.");
        }

        var allCollectDemandsHaveTheSameCollectable = collectDemands
            .Select(cd => cd.CollectableId)
            .Distinct()
            .Count() == 1;

        if (!allCollectDemandsHaveTheSameCollectable)
            throw new InvalidOperationException("All CollectDemands must belong to the same Collectable.");
    }
}