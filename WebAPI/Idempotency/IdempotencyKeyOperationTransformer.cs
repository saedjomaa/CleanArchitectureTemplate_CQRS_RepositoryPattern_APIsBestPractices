using System.Text.Json.Nodes;
using Application.Common.Abstractions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WebAPI.Idempotency;

/// <summary>Documents the required Idempotency-Key header on every action marked <see cref="IdempotentAttribute"/>.</summary>
internal sealed class IdempotencyKeyOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IdempotentAttribute>().Any())
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = IdempotencyFilter.HeaderName,
                In = ParameterLocation.Header,
                Required = true,
                Description = "Required. A unique key for this request (a new UUID per operation). Retrying with the same key returns the first response instead of running the request again.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid", MaxLength = IIdempotencyStore.MaximumKeyLength },

                // An empty example makes API tools (Scalar, client generators) add the header to the request
                // with no value, so it is visible on every POST and the caller only has to fill it in.
                // A real key must never be used as the example: every caller would send the same key.
                Example = JsonValue.Create(string.Empty)
            });
        }

        return Task.CompletedTask;
    }
}
