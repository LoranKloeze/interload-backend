using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Models.Ef;

[Index(nameof(Reference))]
[Index(nameof(UltraOfficeId), IsUnique = true)]

public class Project
{

    public int Id { get; init; }
    public required int UltraOfficeId { get; init; }
    
    [StringLength(256)]
    public required string Reference { get; set; }
    
    [StringLength(256)]
    public required string Name { get; set; }
    [StringLength(256)]
    
    public required string CustomerName { get; set; }
    
    public required DateTime StartAt { get; set; }
    public required DateTime EndAt { get; set; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}