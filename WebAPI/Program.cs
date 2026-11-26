using Application;
using Asp.Versioning;
using Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using WebAPI.Caching;
using WebAPI.Diagnostics;
using WebAPI.Idempotency;
using WebAPI.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, logging) => logging
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers(options => options.Filters.Add<ProblemDetailsLoggingFilter>());

// API versioning: the version is part of the URL (/api/v1/..., /api/v2/...).
builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;   // adds api-supported-versions / api-deprecated-versions response headers
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";         // groups endpoints as "v1", "v2" = the OpenAPI document names below
        options.SubstituteApiVersionInUrl = true;   // OpenAPI shows /api/v1/... instead of /api/v{version}/...
    });

// One OpenAPI document per API version: /openapi/v1.json and /openapi/v2.json.
// Transformers document the Idempotency-Key header on [Idempotent] actions and the 429 response on every action.
foreach (var version in new[] { "v1", "v2" })
{
    builder.Services.AddOpenApi(version, options => options
        .AddOperationTransformer<IdempotencyKeyOperationTransformer>()
        .AddOperationTransformer<TooManyRequestsOperationTransformer>());
}

// Rate limiting per client IP: global limit + "writes" (POST/DELETE) + "external-services" policies (see appsettings).
builder.Services.AddApiRateLimiting(builder.Configuration);

// Output caching of GET responses: "local-data" (expires + evicted by writes through tags) and "external-services".
builder.Services.AddApiCaching(builder.Configuration);

// Idempotency: [Idempotent] actions require an Idempotency-Key header; responses are stored for ExpirationHours.
builder.Services.AddOptions<IdempotencyOptions>().BindConfiguration(IdempotencyOptions.SectionName);
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("School")
    ?? throw new InvalidOperationException("The School connection string is required."),
    builder.Configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"));

var app = builder.Build();
await app.Services.MigrateDatabaseAsync(app.Lifetime.ApplicationStopping);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

// Empty error responses (e.g. 404 for an unknown route or an API version that does not exist for a resource)
// get a ProblemDetails body too, through AddProblemDetails(), so every error has the same shape.
app.UseStatusCodePages();

// After the correlation id, request logging and exception handling, so rejected requests (429) are logged
// with their correlation id; before the controllers, so a rejected request never reaches MVC.
app.UseRateLimiter();

// After the rate limiter, so a response served from the cache still counts toward the client's limit.
// A cache hit is returned here and never reaches MVC (no handler, no database).
app.UseOutputCache();

if (app.Environment.IsDevelopment())
{
    // API documentation is not rate limited.
    app.MapOpenApi().DisableRateLimiting();            // OpenAPI documents: /openapi/v1.json, /openapi/v2.json
    app.MapScalarApiReference(options => options        // Interactive UI with a version dropdown: /scalar
        .AddDocument("v1", "v1 (Courses v1 is deprecated)")
        .AddDocument("v2", "v2", isDefault: true))
        .DisableRateLimiting();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHealthChecks("/health").DisableRateLimiting();   // monitoring must always reach it
app.MapControllers();

app.Run();
