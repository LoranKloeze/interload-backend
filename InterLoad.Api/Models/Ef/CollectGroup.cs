using System.ComponentModel.DataAnnotations;

namespace InterLoad.Models.Ef;

public class CollectGroup // UO: PackingScheme
{
    public int Id { get; init; }
    
    public required int UltraOfficePackingSchemeId { get; init; } 
    
    public bool MergeStockPeriods { get; set; }
    
    [StringLength(256)]
    public required string Title { get; set; }
    
    
    public int ProjectId { get; set; }
    public required Project Project { get; init; }
    
    public bool IsActive { get; set; }
    public List<CollectGroupEntry> CollectGroupEntries { get; set; } = [];
}