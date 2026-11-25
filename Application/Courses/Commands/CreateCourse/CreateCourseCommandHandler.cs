using Application.Common.Abstractions;
using Application.Common.Results;
using Domain.Entities;

namespace Application.Courses.Commands.CreateCourse;

internal sealed class CreateCourseCommandHandler(ICourseRepository courses, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCourseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(CreateCourseCommand command, CancellationToken cancellationToken)
    {
        var course = Course.Create(command.Title, command.Capacity);
        courses.Add(course);
        var result = await unitOfWork.SaveChangesAsync(cancellationToken);

        return result.IsSuccess ? Result<Guid>.Success(course.Id) : Result<Guid>.Failure(result.Error!);
    }
}
