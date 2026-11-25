using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;

namespace Application.Students.Queries.ListStudents;

internal sealed class ListStudentsQueryHandler(IStudentRepository students)
    : IQueryHandler<ListStudentsQuery, Result<PagedResult<StudentResponse>>>
{
    public async Task<Result<PagedResult<StudentResponse>>> HandleAsync(
        ListStudentsQuery query, CancellationToken cancellationToken)
    {
        var page = await students.ListAsync(query.Page, query.PageSize, cancellationToken);

        return Result<PagedResult<StudentResponse>>.Success(page);
    }
}
