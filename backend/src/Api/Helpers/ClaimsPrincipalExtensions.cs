using System.Security.Claims;

namespace Api.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;
            
        var entityType = principal.FindFirst("entity_type")?.Value;
        if (entityType != "user")
            return null;
            
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
            
        return null;
    }
    
    public static bool IsInAnyRole(this ClaimsPrincipal principal, params string[] roles)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return false;
            
        foreach (var role in roles)
        {
            if (principal.IsInRole(role))
                return true;
        }
        
        return false;
    }
    
    public static string? GetEntityType(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst("entity_type")?.Value;
    }
    
    public static bool IsUser(this ClaimsPrincipal principal)
    {
        return GetEntityType(principal) == "user";
    }
    
    public static bool IsClient(this ClaimsPrincipal principal)
    {
        return GetEntityType(principal) == "client";
    }
    
    public static int? GetClientSessionId(this ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;
            
        var entityType = principal.FindFirst("entity_type")?.Value;
        if (entityType != "client")
            return null;
            
        var sessionIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(sessionIdClaim, out var sessionId))
            return sessionId;
            
        return null;
    }
}