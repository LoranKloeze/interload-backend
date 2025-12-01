using System.Text.Json;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Hangfire.Storage;
using InterLoad.Data;
using InterLoad.Models.ApiResponses;
using InterLoad.Models.Ef;
using InterLoad.Services;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Jobs;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncUltraOfficeJob(
    ILogger<SyncUltraOfficeJob> logger,
    IHttpClientFactory httpClientFactory,
    ApplicationDbContext context,
    IConfiguration configuration,
    SyncUltraOfficeToProjectsService syncProjectsService,
    SyncUltraOfficeToSubProjectsService syncSubProjectsService,
    SyncUltraOfficeToCollectablesService syncCollectablesService,
    SyncUltraOfficeToStockPeriodsService syncStockPeriodsService,
    SyncUltraOfficeToCollectGroupsService syncCollectGroupsService,
    SyncUltraOfficeToCollectGroupEntriesService syncCollectGroupEntriesService,
    SyncUltraOfficeToCollectDemandsService syncCollectDemandsService,
    ReconcileActiveCollectGroupsService reconcileActiveCollectGroups
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
            logger.LogInformation($"Starting {nameof(SyncUltraOfficeJob)}...");
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

        var syncId = await LastSyncIdAsync();
        var url = $"{baseUrl}/api/sync_all?since_sync_id={syncId}";
        performContext.WriteLine($"Calling UltraOffice API at: {url}");

        var response = await client.GetFromJsonAsync<SyncUltraOfficeResponse>(url);
        if (response == null)
        {
            throw new InvalidOperationException("Failed to get a valid response from UltraOffice API.");
        }

        if (response.max_sync_id == syncId)
        {
            performContext.WriteLine("No new data to sync, skipping log entry in database.");
            return;
        }

        var logEntry = new UltraOfficeSyncLog
        {
            SyncDate = DateTime.UtcNow,
            UsedSyncId = syncId,
            HangfireJobId = performContext?.BackgroundJob?.Id,
            NextSyncId = response.max_sync_id,
            Payload = JsonSerializer.Serialize(response)
        };
        context.UltraOfficeSyncLogs.Add(logEntry);


        performContext.WriteLine("Updating domain tables in database");
        // The order of these calls may matter due to foreign key constraints

        await syncCollectGroupEntriesService.Delete(response);
        await syncCollectGroupsService.Delete(response);
        await syncCollectDemandsService.Delete(response);
        await syncStockPeriodsService.Delete(response);
        await syncSubProjectsService.Delete(response);
        await syncProjectsService.Delete(response);
        await syncCollectablesService.Delete(response);

        await syncCollectablesService.Upsert(response);
        await syncProjectsService.Upsert(response);
        await syncSubProjectsService.Upsert(response);
        await syncStockPeriodsService.Upsert(response);
        await syncCollectDemandsService.Upsert(response);
        await syncCollectGroupsService.Upsert(response);
        await syncCollectGroupEntriesService.Upsert(response);
        
        await context.SaveChangesAsync();

        await reconcileActiveCollectGroups.Reconcile(response);

        performContext.WriteLine("Updating done");
        
        performContext.WriteLine("Logging sync data to database completed.");
        performContext.WriteLine("Log entry Id: " + logEntry.Id);
    }


    private async Task<long> LastSyncIdAsync()
    {
        var maxSyncId = await context.UltraOfficeSyncLogs
            .MaxAsync(u => (long?)u.NextSyncId) ?? -1;
        if (maxSyncId >= 0)
        {
            return maxSyncId;
        }

        var firstSyncId = configuration.GetValue<string>("UltraOffice:SyncIdForFirstSync") ??
                          throw new InvalidOperationException(
                              "UltraOffice:SyncIdForFirstSync configuration value is missing and there is no previous sync log.");
        return !long.TryParse(firstSyncId, out var parsedSyncId)
            ? throw new InvalidOperationException(
                "UltraOffice:SyncIdForFirstSync configuration value is not a valid long integer.")
            : parsedSyncId;
    }


}