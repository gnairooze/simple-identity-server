using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using SimpleIdentityServer.API.Middleware;

namespace SimpleIdentityServer.API.TagHelpers;

/// <summary>
/// Tag helper that automatically adds CSP nonce to script and style tags.
/// Usage: Add csp-nonce attribute to any script or style tag that needs the nonce.
/// Example: <script csp-nonce>console.log('inline script');</script>
/// </summary>
[HtmlTargetElement("script", Attributes = "csp-nonce")]
[HtmlTargetElement("style", Attributes = "csp-nonce")]
public class CspNonceTagHelper : TagHelper
{
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Remove the csp-nonce attribute (it's just a marker)
        output.Attributes.RemoveAll("csp-nonce");

        // Get the nonce from HttpContext.Items
        var httpContext = ViewContext.HttpContext;
        if (httpContext.Items.TryGetValue(SecurityHeadersMiddleware.CspNonceKey, out var nonceObj) 
            && nonceObj is string nonce)
        {
            // Add the nonce attribute
            output.Attributes.SetAttribute("nonce", nonce);
        }
    }
}

