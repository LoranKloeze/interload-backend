namespace InterLoad.Dtos;

public class CollectDemandDetailsDtoOut
{
    public required int TotalDemand { get; set; }
    public int TotalCollected { get; set; }
    public required string Description { get; set; }
    public required string Remark { get; set; }
}