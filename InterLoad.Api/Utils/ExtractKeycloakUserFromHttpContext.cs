using System.Security.Claims;
using InterLoad.Models;

namespace InterLoad.Utils;

public static class ExtractKeycloakUserFromHttpContext
{
    public static KeycloakUser? Extract(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return null;

        }
        var id = user.Claims
            .FirstOrDefault(c => c.Type is ClaimTypes.NameIdentifier)?
            .Value;
        var name = user.Claims
            .FirstOrDefault(c => c.Type is ClaimTypes.Name)?
            .Value;
        if (id == null || name == null)
        {
            return null;
        }
        
        var guidId = Guid.TryParse(id, out var parsedId) ? parsedId : Guid.Empty;
        
        return new KeycloakUser
        {
            UserId = guidId,
            UserName = name
        };
    }
}