using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WebAPI.RateLimiting;

/// <summary>
/// Documents the 429 response on every API operation: the global rate limiter applies to all of them,
/// so it is added here once instead of a [ProducesResponseType(429)] on every action.
/// </summary>
internal sealed class TooManyRequestsOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd(
            StatusCodes.Status429TooManyRequests.ToString(),
            new OpenApiResponse
            {
                Description = $"Too Many Requests: the client sent too many requests ({RateLimitingSetup.ErrorCode}). " +
                              "Wait the number of seconds in the Retry-After header before retrying."
            });

        return Task.CompletedTask;
    }
}
