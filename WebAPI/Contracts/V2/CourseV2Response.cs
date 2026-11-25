using Application.Common.Responses;

namespace WebAPI.Contracts.V2;

/// <summary>
/// Course as returned by API v2. Adds <see cref="AvailableSeats"/> to the v1 shape.
/// It only reshapes the Application's <see cref="CourseResponse"/>, so it lives in WebAPI:
/// Application stays unaware of API versions.
/// </summary>
public sealed record CourseV2Response(
    Guid Id, string Title, int Capacity, int EnrolledCount, int AvailableSeats, Guid RowVersion);

internal static class CourseV2ResponseMapping
{
    public static CourseV2Response ToV2Response(this CourseResponse course) => new(
        course.Id,
        course.Title,
        course.Capacity,
        course.EnrolledCount,
        AvailableSeats: course.Capacity - course.EnrolledCount,
        course.RowVersion);
}
