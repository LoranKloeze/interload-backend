using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Models.Ef;


[Index(nameof(UltraOfficeArticleId), IsUnique = true)]
[Index(nameof(Location), IsUnique = false)]
public class Collectable
{
    public int Id { get; init; }
    public required int UltraOfficeArticleId { get; init; }
    
    [StringLength(256)]
    public required string Name { get; set; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    
    [StringLength(256)]
    public required string Location { get; set; }

    public decimal Weight { get; set; }
}