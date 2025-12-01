using InterLoad.Data;
using InterLoad.Models.ApiResponses;
using InterLoad.Models.Ef;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Services;

public class SyncUltraOfficeToCollectDemandsService(
    ApplicationDbContext context)
{

    public async Task Upsert(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        foreach (var incomingData in ultraOfficeResponse.upserted.sub_project_articles)
        {
            var existingCollectDemand =
                await context.CollectDemands.FirstOrDefaultAsync(p => 
                    p.UltraOfficeSubProjectArticleId == incomingData.id);

            var subProject = await FindSubProject(incomingData.sub_project_id);
            if (subProject == null)
            {
                throw new Exception(
                    $"SubProject in database expected with UltraOfficeId {incomingData.sub_project_id} but not found");
            }
            var stockProject = await FindStockProject(incomingData.stock_period_id);
            if (stockProject == null)
            {
                throw new Exception(
                    $"StockProject in database expected with UltraOfficeId {incomingData.stock_period_id} but not found");
            }

            if (existingCollectDemand == null)
            {
                Collectable? collectable = null;
                if (incomingData.article_id != null)
                {
                    collectable = await FindCollectable((int)incomingData.article_id);
                    if (collectable == null)
                    {
                        throw new Exception(
                            $"Collectable in database expected with UltraOfficeId {incomingData.article_id} but not found");
                    }
                }
                var newCollectDemand = new CollectDemand
                {
                    UltraOfficeSubProjectArticleId = incomingData.id,
                    SubProjectId = subProject.Id,
                    StockPeriodId = stockProject.Id,
                    CollectableId = collectable?.Id,
                    Remark = incomingData.remark,
                    Demand = incomingData.primary_amount + incomingData.secondary_amount
                };
                context.CollectDemands.Add(newCollectDemand);
            }
            else
            {
                existingCollectDemand.SubProject = subProject;
                existingCollectDemand.StockPeriod = stockProject;
                existingCollectDemand.Remark = incomingData.remark;
                existingCollectDemand.Demand = incomingData.primary_amount + incomingData.secondary_amount;
                context.CollectDemands.Update(existingCollectDemand);
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task<Collectable?> FindCollectable(int incomingDataArticleId)
    {
        return await context.Collectables
            .FirstOrDefaultAsync(p => p.UltraOfficeArticleId == incomingDataArticleId);
    }

    private async Task<StockPeriod?> FindStockProject(int incomingDataStockPeriodId)
    {
        return await context.StockPeriods
            .FirstOrDefaultAsync(p => p.UltraOfficeId == incomingDataStockPeriodId);
    }

    private async Task<SubProject?> FindSubProject(int incomingDataSubProjectId)
    {
        return await context.SubProjects
            .FirstOrDefaultAsync(p => p.UltraOfficeId == incomingDataSubProjectId);
    }

    public async Task Delete(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        var deletedData = ultraOfficeResponse.deleted.FirstOrDefault(d => d.entity == "SubProjectArticle");
        if (deletedData != null)
        {
            await context.CollectDemands
                .Where(p => deletedData.deleted_ids.Contains(p.UltraOfficeSubProjectArticleId))
                .ExecuteDeleteAsync();
        }

        await context.SaveChangesAsync();
    }
}