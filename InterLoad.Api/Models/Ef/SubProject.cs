using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Models.Ef;


[Index(nameof(UltraOfficeId), IsUnique = true)]
public class SubProject
{
    public int Id { get; init; }
    public required int UltraOfficeId { get; init; }
    
    public required Project Project { get; init; }
    
    [StringLength(256)]
    public required string Name { get; set; }
    
    public bool IsCollectable { get; init; }
    
    public SubProject? PrepSubProjectFor { get; set; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}