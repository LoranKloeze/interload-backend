using System.Text.Json;
using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Progress;
using Hangfire.Server;
using Hangfire.Storage;
using InterLoad.Data;
using InterLoad.Models.ApiResponses;
using InterLoad.Models.Ef;
using InterLoad.Services;
using Microsoft.EntityFrameworkCore;
// ReSharper disable UnusedMember.Global

namespace InterLoad.Jobs;

// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassNeverInstantiated.Global
public class ResetAllDataFromUltraOfficeJob(
    ApplicationDbContext context,
    IHttpClientFactory httpClientFactory,
    ILogger<ResetAllDataFromUltraOfficeJob> logger,
    IConfiguration configuration
)
{
    public const string HangfireQueue = "default";

    [AutomaticRetry(Attempts = 0)]
    public async Task StartAsync(PerformContext? performContext)
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            using var distributedLock = connection.AcquireDistributedLock("SyncUltraOfficeJob-lock", TimeSpan.Zero);
            logger.LogInformation($"Starting {nameof(ResetAllDataFromUltraOfficeJob)}...");
            await DoSyncAsync(performContext);
            logger.LogInformation($"Finished {nameof(SyncUltraOfficeJob)}...");
        }
        catch (DistributedLockTimeoutException)
        {
            logger.LogInformation("Previous SyncUltraOfficeJob run still in progress — skipping this execution.");
        }
    }

    private async Task DoSyncAsync(PerformContext? performContext)
    {
        var baseUrl = configuration.GetValue<string>("UltraOffice:BaseUrl") ??
                      throw new InvalidOperationException("UltraOffice:BaseUrl configuration value is missing.");
        var apiKey = configuration.GetValue<string>("UltraOffice:ApiKey") ??
                     throw new InvalidOperationException("UltraOffice:ApiKey configuration value is missing.");

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        var url = $"{baseUrl}/api/all_interload_data";
        performContext.WriteLine($"Calling UltraOffice API at: {url}");
        var response = await client.GetFromJsonAsync<MasterSyncUltraOfficeResponse>(url);
        if (response == null)
        {
            throw new InvalidOperationException("Failed to get a valid response from UltraOffice API.");
        }

        // Delete all
        // await context.CollectActions.ExecuteDeleteAsync();
        await context.UltraOfficeSyncLogs.ExecuteDeleteAsync();
        await context.CollectGroupEntries.ExecuteDeleteAsync();
        await context.CollectGroups.ExecuteDeleteAsync();
        await context.CollectDemands.ExecuteDeleteAsync();
        await context.Collectables.ExecuteDeleteAsync();
        await context.StockPeriods.ExecuteDeleteAsync();
        await context.SubProjects.ExecuteDeleteAsync();
        await context.Projects.ExecuteDeleteAsync();

        var progress = performContext.WriteProgressBar();

        // Re-insert all
        
        // Collectables
        performContext.WriteLine("Upserting Collectables");
        var totalCount = response.articles.Length;
        var count = 0;
        foreach (var incomingArticle in response.articles)
        {
            count++;
            UpdateProgress(progress, count, totalCount);
            context.Collectables.Add(new Collectable
            {
                UltraOfficeArticleId = incomingArticle.id,
                Name = incomingArticle.description,
                Location = incomingArticle.location,
                Weight = incomingArticle.weight ?? 0.00m
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Projects
        performContext.WriteLine("Upserting Projects");

        totalCount = response.projects.Length;
        count = 0;
        foreach (var incomingProject in response.projects)
        {
            count++;
            UpdateProgress(progress, count, totalCount);
#pragma warning disable CA1806
            DateTimeOffset.TryParse(incomingProject.begin_at, out var startAt);
            DateTimeOffset.TryParse(incomingProject.end_at, out var endAt);
#pragma warning restore CA1806
            context.Projects.Add(new Project
            {
                UltraOfficeId = incomingProject.id,
                Name = incomingProject.name,
                Reference = incomingProject.rentman_ref ?? "0000",
                CustomerName = incomingProject.customer_name ?? "ONBEKEND",
                StartAt = startAt.UtcDateTime,
                EndAt = endAt.UtcDateTime
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        // SubProjects
        performContext.WriteLine("Upserting SubProjects");
        totalCount = response.sub_projects.Length;
        count = 0;
        foreach (var incomingSubProject in response.sub_projects)
        {
            count++;
            UpdateProgress(progress, count, totalCount);
            var project =
                await context.Projects.FirstOrDefaultAsync(p => p.UltraOfficeId == incomingSubProject.project_id);
            if (project == null)
            {
                continue;
            }

            context.SubProjects.Add(new SubProject
            {
                UltraOfficeId = incomingSubProject.id,
                Name = incomingSubProject.description,
                Project = project
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        // Reconcile prep subprojects
        performContext.WriteLine("Reconcile prep SubProjects");
        totalCount = response.sub_projects.Length;
        count = 0;
        foreach (var incomingSubProject in response.sub_projects)
        {
            count++;
            UpdateProgress(progress, count, totalCount);
            if (incomingSubProject.prep_list_for_sub_project_id == null)
            {
                continue;
            }
            var subProject = await 
                context.SubProjects.FirstOrDefaultAsync(sp => sp.UltraOfficeId == incomingSubProject.id);
            var prepListForSubProject = 
                await context.SubProjects.FirstOrDefaultAsync(sp =>
                sp.UltraOfficeId == incomingSubProject.prep_list_for_sub_project_id);
            if (subProject == null || prepListForSubProject == null)
            {
                continue;
            }

            subProject.PrepSubProjectFor = prepListForSubProject;

        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // StockPeriods
        performContext.WriteLine("Upserting StockPeriods");
        totalCount = response.stock_periods.Length;
        count = 0;
        foreach (var incomingStockPeriod in response.stock_periods)
        {
            count++;
            UpdateProgress(progress, count, totalCount);
#pragma warning disable CA1806
            DateTimeOffset.TryParse(incomingStockPeriod.begin_at, out var startAt);
            DateTimeOffset.TryParse(incomingStockPeriod.end_at, out var endAt);
#pragma warning restore CA1806
            var project = await context.Projects
                .FirstOrDefaultAsync(p => p.UltraOfficeId == incomingStockPeriod.project_id);
            if (project == null)
            {
                continue;
            }

            context.StockPeriods.Add(new StockPeriod
            {
                UltraOfficeId = incomingStockPeriod.id,
                Name = incomingStockPeriod.reference,
                Project = project,
                StartAt = startAt.UtcDateTime,
                EndAt = endAt.UtcDateTime
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        // CollectGroups
        performContext.WriteLine("Upserting CollectGroups");
        totalCount = response.packing_schemes.Length;
        count = 0;
        foreach (var incomingPackingScheme in response.packing_schemes)
        {
            count++;
            UpdateProgress(progress, count, totalCount);
            var project = await context.Projects
                .FirstOrDefaultAsync(p => p.UltraOfficeId == incomingPackingScheme.project_id);
            if (project == null)
            {
                continue;
            }

            var projectFromResponse = response.projects.FirstOrDefault(p => p.id == incomingPackingScheme.project_id);
            var activePackingSchemeId = projectFromResponse?.active_packing_scheme_id;

            var isActive = activePackingSchemeId == incomingPackingScheme.id;
            
            context.CollectGroups.Add(new CollectGroup
            {
                Project = project,
                IsActive = isActive,
                UltraOfficePackingSchemeId = incomingPackingScheme.id,
                MergeStockPeriods = incomingPackingScheme.merge_stock_periods,
                Title = incomingPackingScheme.title
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        // CollectGroupEntries
        performContext.WriteLine("Upserting CollectGroupEntries");
        totalCount = response.packing_scheme_entries.Length;
        count = 0;
        foreach (var incomingPackingSchemeEntry in response.packing_scheme_entries)
        {
            count++;
            UpdateProgress(progress, count, totalCount);
            var subProject = await context.SubProjects
                .FirstOrDefaultAsync(p => p.UltraOfficeId == incomingPackingSchemeEntry.sub_project_id);
            if (subProject == null)
            {
                continue;
            }
            var collectGroup = await context.CollectGroups
                .FirstOrDefaultAsync(p => p.UltraOfficePackingSchemeId == incomingPackingSchemeEntry.packing_scheme_id);
            if (collectGroup == null)
            {
                continue;
            }

            context.CollectGroupEntries.Add(new CollectGroupEntry
            {
                GroupNr = incomingPackingSchemeEntry.group_nr,
                SubProject = subProject,
                UltraOfficePackingSchemeEntryId = incomingPackingSchemeEntry.id,
                CollectGroup = collectGroup
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // SubProjectArticles
        performContext.WriteLine("Upserting SubProjectArticles");
        var subProjects = await context.SubProjects
            .AsNoTracking()
            .ToDictionaryAsync(p => p.UltraOfficeId);

        var stockPeriods = await context.StockPeriods
            .AsNoTracking()
            .ToDictionaryAsync(p => p.UltraOfficeId);

        var collectables = await context.Collectables
            .AsNoTracking()
            .ToDictionaryAsync(p => p.UltraOfficeArticleId);

        const int batchSize = 1_000;
        var batch = new List<CollectDemand>(batchSize);

        var originalAutoDetectChanges = context.ChangeTracker.AutoDetectChangesEnabled;
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        
        try
        {
            count = 0;
            totalCount = response.sub_project_articles.Length;

            foreach (var incomingSpa in response.sub_project_articles)
            {
                count++;
                UpdateProgress(progress, count, totalCount);

                // Resolve lookups from dictionaries
                if (!subProjects.TryGetValue(incomingSpa.sub_project_id, out var subProject))
                {
                    continue;
                }

                if (!stockPeriods.TryGetValue(incomingSpa.stock_period_id, out var stockPeriod))
                {
                    continue;
                }

                Collectable? collectable = null;
                if (incomingSpa.article_id.HasValue)
                {
                    collectables.TryGetValue(incomingSpa.article_id.Value, out collectable);
                }

                var demand = new CollectDemand
                {
                    UltraOfficeSubProjectArticleId = incomingSpa.id,
                    SubProjectId = subProject.Id,
                    CollectableId = collectable?.Id, // nullable if relation is optional
                    StockPeriodId = stockPeriod.Id,
                    Demand = incomingSpa.primary_amount + incomingSpa.secondary_amount,
                    Remark = incomingSpa.remark
                };

                batch.Add(demand);

                // ReSharper disable once InvertIf
                if (batch.Count >= batchSize)
                {
                    context.CollectDemands.AddRange(batch);
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear();
                    batch.Clear();
                }
            }

            // Flush remaining rows
            if (batch.Count > 0)
            {
                context.CollectDemands.AddRange(batch);
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
                batch.Clear();
            }
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;
        }

        var logEntry = new UltraOfficeSyncLog
        {
            SyncDate = DateTime.UtcNow,
            UsedSyncId = 0,
            HangfireJobId = performContext?.BackgroundJob?.Id,
            NextSyncId = response.max_sync_id,
            Payload = JsonSerializer.Serialize(new { message = "Reset all data from UltraOffice job executed." })
        };

        context.UltraOfficeSyncLogs.Add(logEntry);
        await context.SaveChangesAsync();
    }

    private static void UpdateProgress(IProgressBar progressBar, int count, int totalCount)
    {
        if (totalCount == 0 || count == 0)
        {
            progressBar.SetValue(0);
            return;
        }

        var progress = (double)count / totalCount * 100;
        progressBar.SetValue((int)progress);
    }
}