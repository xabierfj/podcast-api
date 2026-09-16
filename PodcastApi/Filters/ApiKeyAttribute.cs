using Microsoft.AspNetCore.Mvc;

namespace PodcastApi.Filters;

public class ApiKeyAttribute() : ServiceFilterAttribute(typeof(ApiKeyAuthFilter));