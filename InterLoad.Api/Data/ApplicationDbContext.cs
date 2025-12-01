
using InterLoad.Models.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace InterLoad.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    
    public DbSet<UltraOfficeSyncLog> UltraOfficeSyncLogs { get; init; }
    public DbSet<Collectable> Collectables { get; init; }
    public DbSet<CollectDemand> CollectDemands { get; init; }
    public DbSet<Project> Projects { get; init; }
    public DbSet<SubProject> SubProjects { get; init; }
    public DbSet<StockPeriod> StockPeriods { get; init; }
    public DbSet<CollectGroup> CollectGroups { get; init; }
    public DbSet<CollectGroupEntry> CollectGroupEntries { get; init; }
    public DbSet<CollectAction> CollectActions { get; init; }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        ValidateCollectActions();
        
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ValidateCollectActions()
    {
        var noCollectionDemands = ChangeTracker.Entries<CollectAction>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Any(e => e.Entity.CollectDemands.Count == 0);

        if (noCollectionDemands)
            throw new InvalidOperationException("CollectDemands must contain at least one element.");
        
        // The CollectDemands for 1 CollectAction must share the same CollectDemand.Collectable
        var invalidCollectActions = ChangeTracker.Entries<CollectAction>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Where(e => e.Entity.CollectDemands
                .Select(cd => cd.CollectableId)
                .Distinct()
                .Count() > 1)
            .ToList();
        if (invalidCollectActions.Count != 0)
            throw new InvalidOperationException("All CollectDemands in a CollectAction must belong to the same Collectable.");
        
        //And they need to share the same ColletDemand.SubProject
        invalidCollectActions = ChangeTracker.Entries<CollectAction>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Where(e => e.Entity.CollectDemands
                .Select(cd => cd.SubProjectId)
                .Distinct()
                .Count() > 1)
            .ToList();
        if (invalidCollectActions.Count != 0)
            throw new InvalidOperationException("All CollectDemands in a CollectAction must belong to the same SubProject.");
        
    }


    private void SetTimestamps()
    {
        var createdEntities = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added)
            .ToList();

        foreach (
            var property in
            createdEntities
                .Select(entity => entity.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt"))
                .OfType<PropertyEntry>())
        {
            property.CurrentValue = DateTime.Now.ToUniversalTime();
        }

        var updatedEntities = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Modified)
            .ToList();

        foreach (
            var property in
            updatedEntities
                .Select(entity => entity.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt"))
                .OfType<PropertyEntry>())
        {
            property.CurrentValue = DateTime.Now.ToUniversalTime();
        }
    }
    
}