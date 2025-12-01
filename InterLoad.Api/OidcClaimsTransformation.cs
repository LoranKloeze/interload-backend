using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace InterLoad;

public sealed class OidcClaimsTransformation : IClaimsTransformation
{
    private const string MarkerType = "claims_transformed";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } id)
            return Task.FromResult(principal);

        // Idempotency: only transform once
        if (id.HasClaim(c => c.Type == MarkerType))
            return Task.FromResult(principal);

        // Map common claims if missing
        AddIfMissing(id, ClaimTypes.Name, FindFirstValue(principal, "name", ClaimTypes.Name));
        AddIfMissing(id, ClaimTypes.Email, FindFirstValue(principal,
            "email", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", ClaimTypes.Email));
        AddIfMissing(id, ClaimTypes.NameIdentifier, FindFirstValue(principal,
            "sub", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", ClaimTypes.NameIdentifier));

        // Map Keycloak realm roles → ClaimTypes.Role
        var realmAccessJson = principal.FindFirst("realm_access")?.Value;
        if (!string.IsNullOrWhiteSpace(realmAccessJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(realmAccessJson);
                if (doc.RootElement.TryGetProperty("roles", out var rolesElem) &&
                    rolesElem.ValueKind == JsonValueKind.Array)
                {
                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var roleElem in rolesElem.EnumerateArray())
                    {
                        var role = roleElem.GetString();
                        if (string.IsNullOrWhiteSpace(role)) continue;

                        // Avoid duplicates
                        if (!id.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role))
                            id.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                }
            }
            catch
            {
                // Swallow parse errors; don't break auth pipeline
            }
        }

        // Optional: set a marker claim to prevent reprocessing on subsequent requests
        id.AddClaim(new Claim(MarkerType, "true"));

        return Task.FromResult(principal);
    }

    private static void AddIfMissing(ClaimsIdentity id, string claimType, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!id.HasClaim(claimType, value))
            id.AddClaim(new Claim(claimType, value));
    }

    private static string? FindFirstValue(ClaimsPrincipal principal, params string[] types)
    {
        return types.Select(t => principal.FindFirst(t)?.Value)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }
}