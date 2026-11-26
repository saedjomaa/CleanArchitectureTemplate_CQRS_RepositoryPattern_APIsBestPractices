using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OutputCaching;

namespace WebAPI.Caching;

/// <summary>
/// Put on a write action (POST/PUT/PATCH/DELETE): when the action succeeds (2xx), every cached GET response
/// carrying one of the given tags is evicted, so the next GET reads fresh data. Failed writes (4xx/5xx) change
/// nothing, so they evict nothing. Runs before the response is written, so the client that made the write
/// never sees stale data on its next request.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class InvalidatesCacheAttribute(params string[] tags) : Attribute, IAsyncResultFilter
{
    public IReadOnlyList<string> Tags { get; } = tags;

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var statusCode = (context.Result as IStatusCodeActionResult)?.StatusCode ?? context.HttpContext.Response.StatusCode;

        if (statusCode is >= 200 and < 300)
        {
            var store = context.HttpContext.RequestServices.GetRequiredService<IOutputCacheStore>();
            foreach (var tag in Tags)
            {
                await store.EvictByTagAsync(tag, context.HttpContext.RequestAborted);
            }
        }

        await next();
    }
}
