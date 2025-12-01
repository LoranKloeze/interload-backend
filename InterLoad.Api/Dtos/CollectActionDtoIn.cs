using InterLoad.Models;

namespace InterLoad.Dtos;

public class CollectActionDtoIn
{
    public required List<int> CollectDemandIds { get; init; }
    public required int Collected { get; init; }

}