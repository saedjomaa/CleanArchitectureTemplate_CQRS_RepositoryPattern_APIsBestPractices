using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;
using Application.Shared.Errors;

namespace Application.Students.Queries.GetStudent;

internal sealed class GetStudentQueryHandler(IStudentRepository students)
    : IQueryHandler<GetStudentQuery, Result<StudentResponse>>
{
    public async Task<Result<StudentResponse>> HandleAsync(GetStudentQuery query, CancellationToken cancellationToken)
    {
        var student = await students.GetByIdAsync(query.Id, cancellationToken);

        return student is null
            ? Result<StudentResponse>.Failure(StudentErrors.NotFound)
            : Result<StudentResponse>.Success(student);
    }
}
