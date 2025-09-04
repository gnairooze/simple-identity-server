using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace SimpleIdentityServer.API.Authorization;

/// <summary>
/// Attribute to enable field-level authorization on API responses
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class FieldLevelAuthorizationAttribute : ActionFilterAttribute
{
    private readonly string[] _requiredScopes;
    private readonly string[] _requiredRoles;

    public FieldLevelAuthorizationAttribute(string[]? requiredScopes = null, string[]? requiredRoles = null)
    {
        _requiredScopes = requiredScopes ?? Array.Empty<string>();
        _requiredRoles = requiredRoles ?? Array.Empty<string>();
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value != null)
        {
            var user = context.HttpContext.User;
            var filteredValue = FilterResponseBasedOnClaims(objectResult.Value, user);
            objectResult.Value = filteredValue;
        }

        base.OnActionExecuted(context);
    }

    private object FilterResponseBasedOnClaims(object response, ClaimsPrincipal user)
    {
        var userScopes = user.FindAll("scope").Select(c => c.Value).ToList();
        var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var clientId = user.FindFirst("client_id")?.Value;

        // Apply filtering based on client permissions
        return response switch
        {
            IEnumerable<object> collection => collection.Select(item => FilterSingleItem(item, userScopes, userRoles, clientId)),
            _ => FilterSingleItem(response, userScopes, userRoles, clientId)
        };
    }

    private object? FilterSingleItem(object? item, List<string> userScopes, List<string> userRoles, string? clientId)
    {
        if (item == null) return item;

        var itemType = item.GetType();
        var properties = itemType.GetProperties();
        var filteredObject = Activator.CreateInstance(itemType);

        if (filteredObject == null) return item;

        foreach (var property in properties)
        {
            var value = property.GetValue(item);
            
            // Check if property should be included based on authorization rules
            if (ShouldIncludeProperty(property.Name, userScopes, userRoles, clientId))
            {
                property.SetValue(filteredObject, value);
            }
        }

        return filteredObject;
    }

    private bool ShouldIncludeProperty(string propertyName, List<string> userScopes, List<string> userRoles, string? clientId)
    {
        // Define field-level authorization rules
        var sensitiveFields = new Dictionary<string, string[]>
        {
            // Example: Only admin clients can see detailed temperature data
            { "TemperatureC", new[] { "admin", "service" } },
            { "TemperatureF", new[] { "admin", "service" } },
            // Summary and Date are available to all authenticated clients
            { "Summary", new[] { "web_user", "mobile_user", "service", "admin" } },
            { "Date", new[] { "web_user", "mobile_user", "service", "admin" } }
        };

        // Allow all properties if no specific rules defined
        if (!sensitiveFields.ContainsKey(propertyName))
        {
            return true;
        }

        var allowedRoles = sensitiveFields[propertyName];
        
        // Check if user has required roles
        return userRoles.Any(role => allowedRoles.Contains(role)) ||
               (clientId != null && allowedRoles.Contains(clientId));
    }
}
