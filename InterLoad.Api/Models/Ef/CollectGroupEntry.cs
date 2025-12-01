namespace InterLoad.Models.Ef;

public class CollectGroupEntry // UO: PackingSchemeEntry
{
    public int Id { get; init; }
    public int GroupNr { get; set; }
    
    public required int UltraOfficePackingSchemeEntryId { get; init; } 
    
    public required CollectGroup CollectGroup {get; init; }
    public required SubProject SubProject {get; init; }
    public int CollectGroupId { get; set; }
    public int SubProjectId { get; set; }
}