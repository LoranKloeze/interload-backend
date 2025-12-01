using InterLoad.Data;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Controllers.Api;

[Route("/api/[controller]")]
public class SyncLogs(ApplicationDbContext context) : ApiControllerBase
{
    [HttpGet]
    public async Task<List<SyncLogDtoOut>> GetLogs()
    {
        var logs = await context.UltraOfficeSyncLogs
            .OrderByDescending(u => u.Id)
            .Take(100)
            .ToListAsync();
        var dtos = logs.Select(log => new SyncLogDtoOut
        {
            Id = log.Id,
            SyncDate = log.SyncDate,
            UsedSyncId = log.UsedSyncId,
            NextSyncId = log.NextSyncId,
            HangfireJobId = log.HangfireJobId ?? string.Empty,
            PayloadSize = log.Payload.Length
        }).ToList();

        return dtos;
    }

    [HttpGet("{id:int}")]
    public async Task<SyncLogDtoOut?> GetOneLog(int id)
    {
        var log = await context.UltraOfficeSyncLogs
            .FirstOrDefaultAsync(u => u.Id == id);
        if (log == null)
            return null;

        var dto = new SyncLogDtoOut
        {
            Id = log.Id,
            SyncDate = log.SyncDate,
            UsedSyncId = log.UsedSyncId,
            NextSyncId = log.NextSyncId,
            HangfireJobId = log.HangfireJobId ?? string.Empty,
            PayloadSize = log.Payload.Length,
            Payload = log.Payload
        };

        return dto;
    }

    [HttpGet("max-sync-id")]
    public async Task<long> GetMaxSyncId()
    {
        return await context.UltraOfficeSyncLogs
            .MaxAsync(u => (long?)u.NextSyncId) ?? 0;
    }


    public class SyncLogDtoOut
    {
        public required int Id { [UsedImplicitly] get; set; }
        public required DateTime SyncDate { [UsedImplicitly] get; set; }
        public required long UsedSyncId { [UsedImplicitly] get; set; }
        public required long NextSyncId { [UsedImplicitly] get; set; }
        public required string HangfireJobId { [UsedImplicitly] get; set; }
        public required int PayloadSize { [UsedImplicitly] get; set; }
        public string Payload { [UsedImplicitly] get; set; } = "";
    }
}