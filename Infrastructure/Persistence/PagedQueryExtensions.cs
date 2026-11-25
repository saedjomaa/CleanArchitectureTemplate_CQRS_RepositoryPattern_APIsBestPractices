using Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal static class PagedQueryExtensions
{
    public static async Task<PagedResult<T>> ToPageAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var offset = (page - 1L) * pageSize;
        if (offset >= totalCount)
        {
            return new PagedResult<T>([], page, pageSize, totalCount);
        }

        var items = await query.Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
