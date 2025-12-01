using InterLoad.Data;
using InterLoad.Dtos;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Services;

public class CollectOrderServiceWorks(ApplicationDbContext context)
{
    public async Task<List<CollectOrderDtoOut>> GetCurrent(DateTime stockPeriodFrom, DateTime stockPeriodTo)
    {
        var projects = context
            .StockPeriods
            .Where(sp => sp.StartAt >= stockPeriodFrom && sp.StartAt <= stockPeriodTo)
            .Select(sp => sp.Project);


        var regular = await context.CollectGroupEntries
            .Where(cge => projects.Contains(cge.CollectGroup.Project))
            .Where(cge => cge.CollectGroup.IsActive)
            .Where(cge => cge.SubProject.PrepSubProjectFor == null)
            .GroupBy(cge => new
            {
                cge.GroupNr,
                ProjectId = cge.SubProject.Project.Id,
                ProjectReference = cge.SubProject.Project.Reference,
                ProjectName = cge.SubProject.Project.Name,
                ProjectStartAt = cge.SubProject.Project.StartAt
            })
            .OrderBy(group => group.Key.ProjectStartAt)
            .ThenBy(group => group.Key.ProjectName)
            .ThenBy(group => group.Key.GroupNr)
            .Select(group => new CollectOrderDtoOut
            {
                Id = $"{group.Key.ProjectId}-{group.Key.GroupNr}",
                ProjectName = $"IS{group.Key.ProjectReference} - {group.Key.ProjectName}",
                ProjectStart = group.Key.ProjectStartAt,
                ProjectId = group.Key.ProjectId,
                GroupNr = group.Key.GroupNr,
                IsPrep = false
            })
            .ToListAsync();
        
        var prepLists = await context.CollectGroupEntries
            .Where(cge => projects.Contains(cge.CollectGroup.Project))
            .Where(cge => cge.CollectGroup.IsActive)
            .Where(cge => cge.SubProject.PrepSubProjectFor != null)
            .GroupBy(cge => new
            {
                GroupNr = cge.CollectGroup.CollectGroupEntries
                    .Where(e => e.SubProjectId == cge.SubProject.PrepSubProjectFor!.Id)
                    .Select(e => e.GroupNr)
                    .Single(),
                ProjectId = cge.SubProject.Project.Id,
                ProjectReference = cge.SubProject.Project.Reference,
                ProjectName = cge.SubProject.Project.Name,
                ProjectStartAt = cge.SubProject.Project.StartAt
            })
            .OrderBy(group => group.Key.ProjectStartAt)
            .ThenBy(group => group.Key.ProjectName)
            .ThenBy(group => group.Key.GroupNr)
            .Select(group => new CollectOrderDtoOut
            {
                Id = $"{group.Key.ProjectId}-{group.Key.GroupNr}",
                ProjectName = $"IS{group.Key.ProjectReference} - {group.Key.ProjectName}",
                ProjectStart = group.Key.ProjectStartAt,
                ProjectId = group.Key.ProjectId,
                GroupNr = group.Key.GroupNr,
                IsPrep = true
            })
            .ToListAsync();

        return regular.Concat(prepLists).ToList();
    }
}