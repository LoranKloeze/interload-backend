namespace InterLoad.Dtos;

public class CollectOrderEntryDtoOut
{
    public required List<int> CollectDemandIds { get; init; }
    public required int Demand { get; init; }
    public required string Location { get; init; }
    public required string Description { get; set; }
    public required decimal Weight { get; set; }
    public required string Remark { get; set; }
    public required int Collected { get; set; }
    
    public bool JustUpdated { get; set; }
}