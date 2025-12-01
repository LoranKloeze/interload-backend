using JetBrains.Annotations;

namespace InterLoad.Dtos;

public class CollectOrderDtoOut
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required string Id { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required string ProjectName { get; set; }

    public required int GroupNr { [UsedImplicitly] get; set; }
    public required DateTime ProjectStart { [UsedImplicitly] get; set; }
    public required int ProjectId { [UsedImplicitly] get; set; }
    public required bool IsPrep {  [UsedImplicitly] get; set; }
}