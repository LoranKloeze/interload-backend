using InterLoad.Data;
using InterLoad.Models.ApiResponses;
using InterLoad.Models.Ef;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Services;

public class SyncUltraOfficeToSubProjectsService(
    ApplicationDbContext context,
    ILogger<SyncUltraOfficeToSubProjectsService> logger)
{
    public async Task Upsert(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        foreach (var incomingData in ultraOfficeResponse.upserted.sub_projects)
        {
            var existingSubProject =
                await context.SubProjects.FirstOrDefaultAsync(p => p.UltraOfficeId == incomingData.id);

            if (existingSubProject == null)
            {
                var project = await FindProject(incomingData.project_id);
                if (project == null)
                {
                    logger.LogWarning(
                        "Skipping creating SubProject with UltraOfficeId {UltraOfficeId} because its parent Project with UltraOfficeId {ProjectUltraOfficeId} was not found.",
                        incomingData.id, incomingData.project_id);
                    continue;
                }

                var prepListForSubProject = incomingData.prep_list_for_sub_project_id != null
                    ? await FindSubProject((int)incomingData.prep_list_for_sub_project_id)
                    : null;
                var newProject = new SubProject
                {
                    UltraOfficeId = incomingData.id,
                    Name = incomingData.description,
                    Project = project,
                    PrepSubProjectFor = prepListForSubProject
                };
                context.SubProjects.Add(newProject);
            }
            else
            {
                existingSubProject.Name = incomingData.description;
                context.SubProjects.Update(existingSubProject);
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task<Project?> FindProject(int incomingDataProjectId)
    {
        return await context.Projects
            .FirstOrDefaultAsync(p => p.UltraOfficeId == incomingDataProjectId);
    }

    private async Task<SubProject?> FindSubProject(int incomingDataProjectId)
    {
        return await context.SubProjects
            .FirstOrDefaultAsync(p => p.UltraOfficeId == incomingDataProjectId);
    }

    public async Task Delete(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        var deletedData = ultraOfficeResponse.deleted.FirstOrDefault(d => d.entity == "SubProject");
        if (deletedData != null)
        {
            await context.SubProjects
                .Where(p => deletedData.deleted_ids.Contains(p.UltraOfficeId))
                .ExecuteDeleteAsync();
        }

        await context.SaveChangesAsync();
    }
}