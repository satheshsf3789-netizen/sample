using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using S3Manager.Web.Models;
using S3Manager.Web.Services;

namespace S3Manager.Web.Security;

/// <summary>
/// Action filter that ensures an S3 connection is established.
/// Loads the connection from the session; redirects to Home if missing.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireConnectionAttribute : Attribute, IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var credentialStore = context.HttpContext.RequestServices.GetRequiredService<ICredentialStore>();
        var connection = credentialStore.Get(context.HttpContext.Session);

        if (connection == null)
        {
            context.Result = new RedirectToActionResult("Index", "Home", new { flash = "Please connect to S3 first." });
            return;
        }

        context.HttpContext.Items["conn"] = connection;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
