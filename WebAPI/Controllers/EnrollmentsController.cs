using Application.Common.Abstractions;
using Asp.Versioning;
using Application.Common.Pagination;
using Application.Common.Responses;
using Application.Common.Results;
using Application.Enrollments.Commands.CancelEnrollment;
using Application.Enrollments.Commands.EnrollStudent;
using Application.Enrollments.Queries.GetEnrollment;
using Application.Enrollments.Queries.ListStudentCourses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Caching;
using WebAPI.Diagnostics;
using WebAPI.Idempotency;
using WebAPI.RateLimiting;

namespace WebAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/students/{studentId:guid}")]
public sealed class EnrollmentsController : ControllerBase
{
    private const string EnrollmentRoute = "enrollments/{courseId:guid}";

    [HttpPost("enrollments")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    [Idempotent]
    [InvalidatesCache(CacheTags.Enrollments, CacheTags.Courses)] // a course's available seats change too
    [EndpointSummary("Create a course enrollment for a student")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        Guid studentId,
        EnrollStudentRequest request,
        [FromServices] ICommandHandler<EnrollStudentCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new EnrollStudentCommand(studentId, request.CourseId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails(HttpContext);
    }

    [HttpGet(EnrollmentRoute)]
    [OutputCache(PolicyName = CachePolicies.LocalData, Tags = [CacheTags.Enrollments])]
    [EndpointSummary("Get a student's enrollment in a course")]
    [ProducesResponseType<EnrollmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentResponse>> Get(
        Guid studentId,
        Guid courseId,
        [FromServices] IQueryHandler<GetEnrollmentQuery, Result<EnrollmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetEnrollmentQuery(studentId, courseId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails(HttpContext);
    }

    [HttpDelete(EnrollmentRoute)]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    [InvalidatesCache(CacheTags.Enrollments, CacheTags.Courses)] // the seat goes back to the course
    [EndpointSummary("Cancel a student's enrollment and release the seat")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid studentId,
        Guid courseId,
        [FromServices] ICommandHandler<CancelEnrollmentCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CancelEnrollmentCommand(studentId, courseId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails(HttpContext);
    }

    [HttpGet("courses")]
    [OutputCache(PolicyName = CachePolicies.LocalData, Tags = [CacheTags.Enrollments])]
    [EndpointSummary("List courses a student is enrolled in")]
    [ProducesResponseType<PagedResult<StudentCourseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<StudentCourseResponse>>> ListEnrolledCourses(
        Guid studentId,
        [FromServices] IQueryHandler<ListStudentCoursesQuery, Result<PagedResult<StudentCourseResponse>>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize)
    {
        var result = await handler.HandleAsync(new ListStudentCoursesQuery(studentId, page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails(HttpContext);
    }
}

public sealed record EnrollStudentRequest(Guid CourseId);
