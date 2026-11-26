namespace WebAPI.Caching;

/// <summary>Output cache policy names, used with <c>[OutputCache(PolicyName = ...)]</c>.</summary>
public static class CachePolicies
{
    /// <summary>GET over the application's own data: short expiration, invalidated by writes through tags.</summary>
    public const string LocalData = "local-data";

    /// <summary>GET from an external service: longer expiration, no invalidation (we do not own the data).</summary>
    public const string ExternalServices = "external-services";
}

/// <summary>
/// Output cache tags: every cached GET carries the tags of the data it shows, and every write evicts the tags
/// of the data it changes (see <see cref="InvalidatesCacheAttribute"/>).
/// </summary>
public static class CacheTags
{
    public const string Courses = "courses";
    public const string Students = "students";
    public const string Enrollments = "enrollments";
    public const string JsonPlaceholder = "jsonplaceholder";
}
