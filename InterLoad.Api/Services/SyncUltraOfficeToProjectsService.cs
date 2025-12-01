using InterLoad.Data;
using InterLoad.Models.ApiResponses;
using InterLoad.Models.Ef;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Services;

public class SyncUltraOfficeToProjectsService(
    ApplicationDbContext context,
    ILogger<SyncUltraOfficeToProjectsService> logger)
{
    public async Task Upsert(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        foreach (var incomingData in ultraOfficeResponse.upserted.projects)
        {
            var existingProject =
                await context.Projects.FirstOrDefaultAsync(p => p.UltraOfficeId == incomingData.id);

            var (startAt, endAt) = ExtractDates(incomingData);

            if (existingProject == null)
            {
                var newProject = new Project
                {
                    UltraOfficeId = incomingData.id,
                    Reference = incomingData.rentman_ref,
                    Name = incomingData.name,
                    CustomerName = incomingData.customer_name,
                    StartAt = startAt.UtcDateTime,
                    EndAt = endAt.UtcDateTime
                };
                context.Projects.Add(newProject);
            }
            else
            {
                existingProject.Reference = incomingData.rentman_ref;
                existingProject.Name = incomingData.name;
                existingProject.CustomerName = incomingData.customer_name;
                existingProject.StartAt = startAt.UtcDateTime;
                existingProject.EndAt = endAt.UtcDateTime;
                context.Projects.Update(existingProject);
            }
        }

        await context.SaveChangesAsync();
    }

    private (DateTimeOffset startAt, DateTimeOffset endAt) ExtractDates(SyncUltraOfficeResponse.Projects incomingData)
    {
        var parsedBeginAt = DateTimeOffset.TryParse(incomingData.begin_at, out var startAt);
        var parsedEndAt = DateTimeOffset.TryParse(incomingData.end_at, out var endAt);
        if (!parsedBeginAt || !parsedEndAt)
        {
            logger.LogWarning(
                "Invalid date format for Project Id {ProjectId}: begin_at='{BeginAt}', end_at='{EndAt}', using default dates instead.",
                incomingData.id, incomingData.begin_at, incomingData.end_at);
        }

        return (startAt, endAt);
    }
    


    public async Task Delete(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        var deletedData = ultraOfficeResponse.deleted.FirstOrDefault(d => d.entity == "Project");
        if (deletedData != null)
        {
            await context.Projects
                .Where(p => deletedData.deleted_ids.Contains(p.UltraOfficeId))
                .ExecuteDeleteAsync();
        }

        await context.SaveChangesAsync();
    }
}