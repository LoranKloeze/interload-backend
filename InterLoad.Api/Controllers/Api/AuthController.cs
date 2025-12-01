using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace InterLoad.Controllers.Api;

[ApiController]
[Route("/api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AuthController : ControllerBase
{
    
    // ReSharper disable once RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute
    [HttpPost("cookie")]
    [Authorize]
    public async Task<Results<Ok, UnauthorizedHttpResult>> TokenToCookie()
    {
        try
        {
            var principal = HttpContext.User;

            var identity = new ClaimsIdentity(principal.Identity);

            var roleClaims = principal.Claims.Where(c => c.Type is "role" or "roles").ToList();
            if (roleClaims.Count != 0)
            {
                foreach (var rc in roleClaims)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, rc.Value));
                }
            }

            // Optionally minimize claims set for cookie to reduce size:
            var minimalClaims = new List<Claim>
            {
                // prefer NameIdentifier if present
                identity.FindFirst(ClaimTypes.NameIdentifier) ?? identity.FindFirst("sub") ??
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            var nameClaim = identity.FindFirst(ClaimTypes.Name) ?? identity.FindFirst("name");
            if (nameClaim != null) minimalClaims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));

            // carry roles
            minimalClaims.AddRange(identity.FindAll(ClaimTypes.Role)
                .Select(role => new Claim(ClaimTypes.Role, role.Value)));

            var minimalIdentity = new ClaimsIdentity(minimalClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var cookiePrincipal = new ClaimsPrincipal(minimalIdentity);

            // 4) Sign in - issue cookie
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, cookiePrincipal,
                authProperties);

            return TypedResults.Ok();
        }
        catch (SecurityTokenException)
        {
            return TypedResults.Unauthorized();
        }
    }

    // ReSharper disable once RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute
    [HttpDelete("cookie")]
    [AllowAnonymous]
    public async Task<Ok> DeleteCookie()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return TypedResults.Ok();
    }
}