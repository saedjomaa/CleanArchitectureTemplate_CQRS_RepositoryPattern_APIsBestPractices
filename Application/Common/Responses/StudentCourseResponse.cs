namespace Application.Common.Responses;

public sealed record StudentCourseResponse(Guid CourseId, string Title, DateTimeOffset EnrolledAt);
