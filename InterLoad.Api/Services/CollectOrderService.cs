using InterLoad.Data;
using InterLoad.Dtos;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Services;

public class CollectOrderService(ApplicationDbContext context)
{
    public async Task<List<CollectOrderDtoOut>> GetCurrent(DateTime stockPeriodFrom, DateTime stockPeriodTo)
    {
        var projectIds = context
            .StockPeriods
            .Where(sp => sp.StartAt >= stockPeriodFrom && sp.StartAt <= stockPeriodTo)
            .Select(sp => sp.Project.Id);

        var result = await context.CollectGroupEntries
            .Where(cge => projectIds.Contains(cge.CollectGroup.Project.Id))
            .Where(cge => cge.CollectGroup.IsActive)
            .Select(cge => new
            {
                Entry = cge,
                IsPrep = cge.SubProject.PrepSubProjectFor != null,
                EffectiveGroupNr = cge.SubProject.PrepSubProjectFor == null
                    ? cge.GroupNr
                    : cge.CollectGroup.CollectGroupEntries
                        .Where(e => e.SubProjectId == cge.SubProject.PrepSubProjectFor!.Id)
                        .Select(e => e.GroupNr)
                        .Single()
            })
            .GroupBy(x => new
            {
                GroupNr = x.EffectiveGroupNr,
                x.IsPrep,
                ProjectId = x.Entry.SubProject.Project.Id,
                ProjectReference = x.Entry.SubProject.Project.Reference,
                ProjectName = x.Entry.SubProject.Project.Name,
                ProjectStartAt = x.Entry.SubProject.Project.StartAt
            })
            .OrderBy(g => g.Key.ProjectStartAt)
            .ThenBy(g => g.Key.ProjectName)
            .ThenBy(g => g.Key.GroupNr)
            .ThenBy(g => g.Key.IsPrep)
            .Select(g => new CollectOrderDtoOut
            {
                Id = $"{g.Key.ProjectId}-{g.Key.GroupNr}-{g.Key.IsPrep}",
                ProjectName = $"IS{g.Key.ProjectReference} - {g.Key.ProjectName}",
                ProjectStart = g.Key.ProjectStartAt,
                ProjectId = g.Key.ProjectId,
                GroupNr = g.Key.GroupNr,
                IsPrep = g.Key.IsPrep
            })
            .ToListAsync();

        return result;
    }

}