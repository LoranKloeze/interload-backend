using InterLoad.Data;
using InterLoad.Models.ApiResponses;
using InterLoad.Models.Ef;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Services;

public class SyncUltraOfficeToCollectGroupsService(
    ApplicationDbContext context,
    ILogger<SyncUltraOfficeToCollectGroupsService> logger)
{
    public async Task Upsert(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        foreach (var incomingData in ultraOfficeResponse.upserted.packing_schemes)
        {
            var existingCollectGroup =
                await context.CollectGroups.FirstOrDefaultAsync(p => p.UltraOfficePackingSchemeId == incomingData.id);

            var project = await FindProject(incomingData.project_id);
            if (project == null)
            {
                logger.LogWarning("Skipping creating CollectGroup with UltraOfficeId {UltraOfficeId} because its parent Project with UltraOfficeId {ProjectUltraOfficeId} was not found.",
                    incomingData.id, incomingData.project_id);
                continue;
            }
            if (existingCollectGroup == null)
            {
                var newCollectGroup = new CollectGroup
                {
                    Project = project,
                    UltraOfficePackingSchemeId = incomingData.id,
                    Title = incomingData.title,
                    MergeStockPeriods = incomingData.merge_stock_periods

                };
                context.CollectGroups.Add(newCollectGroup);
            }
            else
            {
                existingCollectGroup.Title = incomingData.title;
                existingCollectGroup.MergeStockPeriods = incomingData.merge_stock_periods;
                context.CollectGroups.Update(existingCollectGroup);
            }
        }

        await context.SaveChangesAsync();
    }
    
    private async Task<Project?> FindProject(int incomingDataProjectId)
    {
        return await context.Projects
            .FirstOrDefaultAsync(p => p.UltraOfficeId == incomingDataProjectId);
    }

    public async Task Delete(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        var deletedData = ultraOfficeResponse.deleted.FirstOrDefault(d => d.entity == "PackingScheme");
        if (deletedData != null)
        {
            await context.CollectGroups
                .Where(p => deletedData.deleted_ids.Contains(p.UltraOfficePackingSchemeId))
                .ExecuteDeleteAsync();
        }

        await context.SaveChangesAsync();
    }
}