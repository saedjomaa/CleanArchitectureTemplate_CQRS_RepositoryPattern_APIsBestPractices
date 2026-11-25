using Application.Common.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Idempotency;

/// <summary>Writes a stored response again, byte for byte, and marks it with <c>Idempotency-Replayed: true</c>.</summary>
internal sealed class IdempotentReplayResult(IdempotentResponse response) : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var httpResponse = context.HttpContext.Response;
        httpResponse.StatusCode = response.StatusCode;
        httpResponse.Headers[IdempotencyFilter.ReplayedHeaderName] = "true";

        if (response.ContentType is not null)
        {
            httpResponse.ContentType = response.ContentType;
        }

        if (response.Location is not null)
        {
            httpResponse.Headers.Location = response.Location;
        }

        await httpResponse.Body.WriteAsync(response.Body, context.HttpContext.RequestAborted);
    }
}
