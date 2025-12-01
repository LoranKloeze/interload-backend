using InterLoad.Data;
using InterLoad.Models.ApiResponses;
using InterLoad.Models.Ef;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Services;

public class SyncUltraOfficeToCollectGroupEntriesService(
    ApplicationDbContext context,
    ILogger<SyncUltraOfficeToCollectGroupEntriesService> logger)
{
    public async Task Upsert(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        foreach (var incomingData in ultraOfficeResponse.upserted.packing_scheme_entries)
        {
            var existingCollectGroupEntry =
                await context.CollectGroupEntries.FirstOrDefaultAsync(p => p.UltraOfficePackingSchemeEntryId == incomingData.id);

            var subProject = await FindProject(incomingData.sub_project_id);
            if (subProject == null)
            {
                logger.LogWarning("Skipping creating CollectGroupEntry with UltraOfficeId {UltraOfficeId} because its parent SubProject with UltraOfficeId {ProjectUltraOfficeId} was not found.",
                    incomingData.id, incomingData.sub_project_id);
                continue;
            }
            
            var collectGroup = await FindCollectGroup(incomingData.packing_scheme_id);
            if (collectGroup == null)
            {
                logger.LogWarning("Skipping creating CollectGroupEntry with UltraOfficeId {UltraOfficeId} because its parent CollectGroup with UltraOfficeId {ProjectUltraOfficeId} was not found.",
                    incomingData.id, incomingData.sub_project_id);
                continue;
            }
            if (existingCollectGroupEntry == null)
            {
                var newCollectGroupEntry = new CollectGroupEntry
                {
                    GroupNr = incomingData.group_nr,
                    UltraOfficePackingSchemeEntryId = incomingData.id,
                    CollectGroup = collectGroup,
                    SubProject = subProject
                };
                context.CollectGroupEntries.Add(newCollectGroupEntry);
            }
            else
            {
                existingCollectGroupEntry.GroupNr = incomingData.group_nr;
                context.CollectGroupEntries.Update(existingCollectGroupEntry);
            }
        }

        await context.SaveChangesAsync();
    }

    
    private async Task<SubProject?> FindProject(int incomingDataSubProjectId)
    {
        return await context.SubProjects
            .FirstOrDefaultAsync(p => p.UltraOfficeId == incomingDataSubProjectId);
    }
    
    private async Task<CollectGroup?> FindCollectGroup(int incomingPackingSchemeId)
    {
        return await context.CollectGroups
            .FirstOrDefaultAsync(p => p.UltraOfficePackingSchemeId == incomingPackingSchemeId);
    }

    public async Task Delete(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        var deletedData = ultraOfficeResponse.deleted.FirstOrDefault(d => d.entity == "PackingSchemeEntry");
        if (deletedData != null)
        {
            await context.CollectGroupEntries
                .Where(p => deletedData.deleted_ids.Contains(p.UltraOfficePackingSchemeEntryId))
                .ExecuteDeleteAsync();
        }

        await context.SaveChangesAsync();
    }
}