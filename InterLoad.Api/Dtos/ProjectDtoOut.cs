using JetBrains.Annotations;

namespace InterLoad.Dtos;

public class ProjectDtoOut
{
    public int Id { [UsedImplicitly] get; set; }
    public string Name { [UsedImplicitly] get; set; } = null!;
}