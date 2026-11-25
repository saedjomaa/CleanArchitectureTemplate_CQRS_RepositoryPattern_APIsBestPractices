using Application.Common.Abstractions;
using Application.Common.Results;
using Domain.Entities;

namespace Application.Students.Commands.CreateStudent;

internal sealed class CreateStudentCommandHandler(IStudentRepository students, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateStudentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(CreateStudentCommand command, CancellationToken cancellationToken)
    {
        var student = Student.Create(command.FullName);
        students.Add(student);
        var result = await unitOfWork.SaveChangesAsync(cancellationToken);

        return result.IsSuccess ? Result<Guid>.Success(student.Id) : Result<Guid>.Failure(result.Error!);
    }
}
