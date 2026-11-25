using Application.Common.Pagination;

namespace Application.Students.Queries.ListStudents;

public sealed record ListStudentsQuery(int Page = Pagination.DefaultPage, int PageSize = Pagination.DefaultPageSize);
