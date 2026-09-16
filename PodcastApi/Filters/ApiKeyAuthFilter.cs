using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PodcastApi.Filters;

public class ApiKeyAuthFilter(IConfiguration config) : IAuthorizationFilter
{
    private const string ApiKeyHeaderName = "x-api-key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()) 
            return;
        var validApiKey = config["ApiKey"];
        if (string.IsNullOrEmpty(validApiKey) 
            || !context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey) 
            || !FixedTimeEquals(providedKey.ToString(), validApiKey)) context.Result = new UnauthorizedResult();
    }

    private static bool FixedTimeEquals(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));
}