namespace Application.Enrollments.Commands.EnrollStudent;

public sealed record EnrollStudentCommand(Guid StudentId, Guid CourseId);
