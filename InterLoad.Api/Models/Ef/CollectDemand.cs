using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Models.Ef;


[Index(nameof(UltraOfficeSubProjectArticleId), IsUnique = true)]
public class CollectDemand
{
    public int Id { get; init; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public SubProject SubProject { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public required int UltraOfficeSubProjectArticleId { get; init; } 
    
    public Collectable? Collectable { get; init; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public StockPeriod StockPeriod { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    [StringLength(1024)] 
    public string Remark { get; set; } = "";
    
    public int Demand { get; set; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public required int SubProjectId { get; init; }
    public int? CollectableId { get; init; }
    public required int StockPeriodId { get; init; }


    public List<CollectAction> CollectActions { get; set; } = [];
}