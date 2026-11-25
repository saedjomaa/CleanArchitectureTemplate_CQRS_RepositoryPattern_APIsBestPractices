namespace Application.Enrollments.Commands.CancelEnrollment;

public sealed record CancelEnrollmentCommand(Guid StudentId, Guid CourseId);
