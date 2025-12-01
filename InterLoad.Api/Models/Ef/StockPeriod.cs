using System.ComponentModel.DataAnnotations;

namespace InterLoad.Models.Ef;

public class StockPeriod
{
    public int Id { get; init; }
    public required int UltraOfficeId { get; init; }
    public required Project Project { get; init; }
    
    [StringLength(256)]
    public required string Name { get; set; }
    public required DateTime StartAt { get; set; }
    public required DateTime EndAt { get; set; }
}