namespace InterLoad.Dtos;

public class CollectActionDtoOut
{
    public required int Id { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string UserName { get; set; }
    public required string ProjectName { get; set; }
    public required string SubProjectName { get; set; }
    public required string CollectableName { get; set; }
    public required int Collected { get; set; }
}