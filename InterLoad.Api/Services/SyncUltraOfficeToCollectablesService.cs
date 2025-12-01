using InterLoad.Data;
using InterLoad.Models.ApiResponses;
using InterLoad.Models.Ef;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Services;

public class SyncUltraOfficeToCollectablesService(
    ApplicationDbContext context)
{

    public async Task Upsert(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        foreach (var incomingData in ultraOfficeResponse.upserted.articles)
        {
            var existingCollectable =
                await context.Collectables.FirstOrDefaultAsync(p => p.UltraOfficeArticleId == incomingData.id);

            if (existingCollectable == null)
            {
                var newCollectable = new Collectable
                {
                    UltraOfficeArticleId = incomingData.id,
                    Name = incomingData.description,
                    Location = incomingData.location,
                    Weight = incomingData.weight ?? 0.00m
                };
                context.Collectables.Add(newCollectable);
            }
            else
            {
                existingCollectable.Name = incomingData.description;
                existingCollectable.Location = incomingData.location;
                existingCollectable.Weight = incomingData.weight ?? 0.00m;
                context.Collectables.Update(existingCollectable);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task Delete(SyncUltraOfficeResponse ultraOfficeResponse)
    {
        var deletedData = ultraOfficeResponse.deleted.FirstOrDefault(d => d.entity == "Article");
        if (deletedData != null)
        {
            await context.SubProjects
                .Where(p => deletedData.deleted_ids.Contains(p.UltraOfficeId))
                .ExecuteDeleteAsync();
        }

        await context.SaveChangesAsync();
    }
}