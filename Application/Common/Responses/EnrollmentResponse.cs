namespace Application.Common.Responses;

public sealed record EnrollmentResponse(Guid StudentId, Guid CourseId, DateTimeOffset EnrolledAt);
