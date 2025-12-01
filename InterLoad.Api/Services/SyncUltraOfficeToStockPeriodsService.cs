using InterLoad.Data;
using InterLoad.Models.ApiResponses;
using InterLoad.Models.Ef;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Services;

public class SyncUltraOfficeToStockPeriodsService(
    ApplicationDbContext context,
    ILogger<SyncUltraOfficeToStockPeriodsService> logger)
{

    public async Task Upsert(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        foreach (var incomingData in ultraOfficeResponse.upserted.stock_periods)
        {
            var existingStockPeriod =
                await context.StockPeriods.FirstOrDefaultAsync(p => p.UltraOfficeId == incomingData.id);

            var (startAt, endAt) = ExtractDates(incomingData);
            var project = await FindProject(incomingData.project_id);
            if (project == null)
            {
                logger.LogWarning("Skipping creating StockPeriod with UltraOfficeId {UltraOfficeId} because its parent Project with UltraOfficeId {ProjectUltraOfficeId} was not found.",
                    incomingData.id, incomingData.project_id);
                continue;
            }
            if (existingStockPeriod == null)
            {
                var newStockPeriod = new StockPeriod
                {
                    UltraOfficeId = incomingData.id,
                    Name = incomingData.reference,
                    StartAt = startAt.UtcDateTime,
                    EndAt = endAt.UtcDateTime,
                    Project = project
                };
                context.StockPeriods.Add(newStockPeriod);
            }
            else
            {
                existingStockPeriod.Name = incomingData.reference;
                existingStockPeriod.StartAt = startAt.UtcDateTime;
                existingStockPeriod.EndAt = endAt.UtcDateTime;
                context.StockPeriods.Update(existingStockPeriod);
            }
        }

        await context.SaveChangesAsync();
    }

    private (DateTimeOffset startAt, DateTimeOffset endAt) ExtractDates(SyncUltraOfficeResponse.Stock_periods incomingData)
    {
        var parsedBeginAt = DateTimeOffset.TryParse(incomingData.begin_at, out var startAt);
        var parsedEndAt = DateTimeOffset.TryParse(incomingData.end_at, out var endAt);
        if (!parsedBeginAt || !parsedEndAt)
        {
            logger.LogWarning(
                "Invalid date format for StockPeriod Id {ProjectId}: begin_at='{BeginAt}', end_at='{EndAt}', using default dates instead.",
                incomingData.id, incomingData.begin_at, incomingData.end_at);
        }

        return (startAt, endAt);
    }
    
    private async Task<Project?> FindProject(int incomingDataProjectId)
    {
        return await context.Projects
            .FirstOrDefaultAsync(p => p.UltraOfficeId == incomingDataProjectId);
    }

    public async Task Delete(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        var deletedData = ultraOfficeResponse.deleted.FirstOrDefault(d => d.entity == "StockPeriod");
        if (deletedData != null)
        {
            await context.StockPeriods
                .Where(p => deletedData.deleted_ids.Contains(p.UltraOfficeId))
                .ExecuteDeleteAsync();
        }

        await context.SaveChangesAsync();
    }
}