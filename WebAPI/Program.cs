using Application;
using Asp.Versioning;
using Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using WebAPI.Diagnostics;

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

// One OpenAPI document per API version: /openapi/v1.json and /openapi/v2.json
builder.Services.AddOpenApi("v1");
builder.Services.AddOpenApi("v2");
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
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // OpenAPI documents: /openapi/v1.json, /openapi/v2.json
    app.MapScalarApiReference(options => options    // Interactive UI with a version dropdown: /scalar
        .AddDocument("v1", "v1 (Courses v1 is deprecated)")
        .AddDocument("v2", "v2", isDefault: true));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
