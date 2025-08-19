using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Resource.API.Services;

public class ScopeClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var scopeClaim = principal.FindFirst("scope");
        if (scopeClaim != null && scopeClaim.Value.Contains(' '))
        {
            var claimsIdentity = (ClaimsIdentity)principal.Identity!;
            
            // Remove the original space-separated scope claim
            claimsIdentity.RemoveClaim(scopeClaim);
            
            // Add individual scope claims
            var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var scope in scopes)
            {
                claimsIdentity.AddClaim(new Claim("scope", scope));
            }
        }
        
        return Task.FromResult(principal);
    }
}
