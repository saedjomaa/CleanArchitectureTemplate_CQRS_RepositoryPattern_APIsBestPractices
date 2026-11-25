using Application.Common.Abstractions;
using Application.Common.Pagination;
using Application.Common.Responses;
using Application.Common.Results;
using Application.Courses.Commands.CreateCourse;
using Application.Courses.Queries.GetCourse;
using Application.Courses.Queries.ListCourses;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Contracts.V2;
using WebAPI.Diagnostics;
using WebAPI.Idempotency;
using WebAPI.RateLimiting;

namespace WebAPI.Controllers;

/// <summary>
/// Courses exist in two API versions:
/// - v1 (deprecated): GET returns <see cref="CourseResponse"/>.
/// - v2: GET returns <see cref="CourseV2Response"/>, which adds availableSeats.
/// Create and List are the same in both versions (no [MapToApiVersion], so they serve every declared version).
/// </summary>
[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/courses")]
public sealed class CoursesController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    [Idempotent]
    [EndpointSummary("Create a course")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Guid>> Create(
        CreateCourseCommand command,
        [FromServices] ICommandHandler<CreateCourseCommand, Result<Guid>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        // The version in the Location URL comes from the current request (v1 or v2),
        // and both GET actions share the same route template.
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value }, result.Value)
            : result.ToProblemDetails(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Get a course by ID (v1, deprecated: use v2)")]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseResponse>> Get(
        Guid id,
        [FromServices] IQueryHandler<GetCourseQuery, Result<CourseResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCourseQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [MapToApiVersion("2.0")]
    [EndpointSummary("Get a course by ID, including available seats")]
    [ProducesResponseType<CourseV2Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseV2Response>> GetV2(
        Guid id,
        [FromServices] IQueryHandler<GetCourseQuery, Result<CourseResponse>> handler,
        CancellationToken cancellationToken)
    {
        // Same query as v1; only the response shape changes.
        var result = await handler.HandleAsync(new GetCourseQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value.ToV2Response()) : result.ToProblemDetails(HttpContext);
    }

    [HttpGet]
    [EndpointSummary("List courses")]
    [ProducesResponseType<PagedResult<CourseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CourseResponse>>> List(
        [FromServices] IQueryHandler<ListCoursesQuery, Result<PagedResult<CourseResponse>>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize)
    {
        var result = await handler.HandleAsync(new ListCoursesQuery(page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails(HttpContext);
    }
}
