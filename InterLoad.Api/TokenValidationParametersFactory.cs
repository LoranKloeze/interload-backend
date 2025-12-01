using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace InterLoad;

public static class TokenValidationParametersFactory
{
    public static TokenValidationParameters Create(string issuer, string validAudience)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            AudienceValidator = (audiences, _, _) =>
            {
                return audiences is not null && 
                       audiences.Any(aud => string.Equals(aud, validAudience, StringComparison.Ordinal));
            },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    }
}
