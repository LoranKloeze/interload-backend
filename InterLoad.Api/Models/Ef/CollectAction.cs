using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace InterLoad.Models.Ef;

[Index(nameof(KeycloakUserId))]
[Index(nameof(KeycloakUserName))]
public class CollectAction
{
    public int Id { get; set; }
    public required List<CollectDemand> CollectDemands { get; set; }
    public int Collected { get; set; }
    
    public required Guid KeycloakUserId { get; set; }
    
    [StringLength(64)]
    public required string KeycloakUserName { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}