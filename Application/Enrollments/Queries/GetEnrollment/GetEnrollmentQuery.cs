namespace Application.Enrollments.Queries.GetEnrollment;

public sealed record GetEnrollmentQuery(Guid StudentId, Guid CourseId);
