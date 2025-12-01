namespace InterLoad.Models;

public class KeycloakUser
{
    public required Guid UserId { get; init; }
    public required string UserName { get; init; }
}