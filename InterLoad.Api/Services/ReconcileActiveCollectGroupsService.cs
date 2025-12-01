using InterLoad.Data;
using InterLoad.Models.ApiResponses;
using InterLoad.Models.Ef;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Services;

public class ReconcileActiveCollectGroupsService(ApplicationDbContext context)
{
    public async Task Reconcile(SyncUltraOfficeResponse response)
    {
        foreach (var incomingProject in response.upserted.projects)
        {
            var project = await FindProject(incomingProject.id);
            if (project == null)
            {
                continue;
            }

            // Set all collect groups for this project to inactive
            var existingGroups = context.CollectGroups.Where(cg => cg.Project == project);
            await foreach (var existingGroup in existingGroups.AsAsyncEnumerable())
            {
                existingGroup.IsActive = false;
            }

            if (incomingProject.active_packing_scheme_id == null)
            {
                continue;
            }
            var activeCollectGroup = await FindCollectGroup((int)incomingProject.active_packing_scheme_id);
            if (activeCollectGroup == null)
            {
                continue;
            }

            activeCollectGroup.IsActive = true;
            await context.SaveChangesAsync();
        }

        
    }

    private async Task<Project?> FindProject(int incomingDataProjectId)
    {
        return await context.Projects
            .FirstOrDefaultAsync(p => p.UltraOfficeId == incomingDataProjectId);
    }

    private async Task<CollectGroup?> FindCollectGroup(int incomingPackingSchemeId)
    {
        return await context.CollectGroups
            .FirstOrDefaultAsync(p => p.UltraOfficePackingSchemeId == incomingPackingSchemeId);
    }
}