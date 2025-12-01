
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterLoad.Models.Ef;

public class UltraOfficeSyncLog
{
    
    public int Id { get; init; }
    public required DateTime SyncDate { get; init; }
    
    public required long UsedSyncId { get; init; }
    public required long NextSyncId { get; init; }
    
    [Column(TypeName = "jsonb")]
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public required string Payload { get; init; }

    [StringLength(12)]
    public string? HangfireJobId { get; init; }
}