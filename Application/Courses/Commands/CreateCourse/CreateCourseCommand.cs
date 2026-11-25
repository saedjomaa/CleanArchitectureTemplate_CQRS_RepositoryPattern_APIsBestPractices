using Domain.Entities;

namespace Application.Courses.Commands.CreateCourse;

public sealed record CreateCourseCommand(string Title, int Capacity = Course.DefaultCapacity);
