# AGENTS.md

Business rules, the endpoint list and project-specific technical facts (database provider, the value of `[Project]`, known exceptions) live in `Requirements.md`. Read it before adding or changing a use case. This file describes how the code is written and is the same for every project that uses this architecture.

Placeholders used below: `[Project]` (project name prefix, e.g. for the `DbContext`), `[Module]` (feature folder, plural), `[Entity]` (domain entity), `[UseCaseName]`, `[Action]`, `[Property]`, `[Reason]`.

## Stack
- ASP.NET Core Web API with **Controllers**. The target framework, nullable and implicit usings are set once in `Directory.Build.props`.
- Clean Architecture + CQRS + Repository Pattern with Unit of Work
- **No MediatR/Mediator. No AutoMapper** (direct handler injection, manual mapping only)
- FluentValidation, EF Core, Scrutor, Serilog

## Layers (dependencies point inward only)
- `Domain`: entities, enums, domain exceptions. References nothing.
- `Application`: commands, queries, handlers, validators, responses, results, repository interfaces and `IUnitOfWork`. References Domain only. **Application has no EF Core package reference**; keep it that way.
- `Infrastructure`: EF Core, `DbContext`, repository and Unit of Work implementations, migrations, external services. Implements Application interfaces.
- `WebAPI`: controllers, `Program.cs`. Composition root.

Handlers depend on repository interfaces and `IUnitOfWork`, never on `DbContext` or other Infrastructure types.

## Data Access (Repositories and Unit of Work)
- Repository interfaces and `IUnitOfWork` live in `Application/Common/Abstractions`. Implementations live in `Infrastructure/Persistence/Repositories` (repositories) and `Infrastructure/Persistence` (`[Project]UnitOfWork`).
- One repository per aggregate or feature area: `I[Entity]Repository`.
- `DbContext`, `DbSet<T>`, `IQueryable<T>` and provider-specific types never leave Infrastructure. Repository methods return domain entities, response records from `Application/Common/Responses`, `PagedResult<T>`, `bool`, or small records that group what a command needs (e.g. `[Entity]For[UseCaseName]`).
- Method kinds:
  - **Writes**: `Add` / `Remove` only stage changes. They never call `SaveChangesAsync`.
  - **Loads for a command**: return tracked domain entities, named after the use case that needs them (`GetFor[UseCaseName]Async`).
  - **Reads for a query**: return response records directly (`GetByIdAsync`, `ListAsync`).
  - **Checks**: `ExistsAsync` returns `bool`.
- Before adding a new repository method, check whether an existing one already fits. Do not add a generic repository (`IRepository<T>`).
- `IUnitOfWork.SaveChangesAsync` commits once per command and returns `Result`. It translates persistence failures in one place:
  - `DbUpdateConcurrencyException` → `PersistenceErrors.ConcurrencyConflict`
  - Provider-specific primary-key or unique violation → `PersistenceErrors.DuplicateKey`
- Handlers translate a `PersistenceErrors` value to the module's own error when the meaning is known (e.g. `DuplicateKey` → `[Module]Errors.[Reason]`). Otherwise they return the result as is. Handlers never use `try/catch` for persistence errors.
- Repositories are registered automatically as scoped services in `AddInfrastructure()` with Scrutor: every class in the Infrastructure assembly whose name ends with `Repository` is registered under its matching interface (`AsMatchingInterface`: `[Entity]Repository` → `I[Entity]Repository`). No manual registration is needed, but the class and interface names must match exactly. A class ending with `Repository` without a matching interface is not registered. Keep repositories in `Infrastructure/Persistence/Repositories` for organization; the scan does not depend on the folder.
- `IUnitOfWork` is registered manually (`[Project]UnitOfWork` does not follow the `I[ClassName]` naming). Everything is scoped, so repositories and the Unit of Work share the same `DbContext` per request.

## EF Core Entity Configuration
- Each persisted entity must define its EF Core mapping in `[Entity]Configuration.cs` under `Infrastructure/Persistence/Configurations` using `IEntityTypeConfiguration<[Entity]>`.
- `OnModelCreating` must apply these configurations and, when the project has seed data, call `[Project]Seed.Configure(modelBuilder)`.
- Column lengths come from the entity's constants (e.g. `[Entity].Maximum[Property]Length`), never from literal numbers.
- Enforce uniqueness in the database (keys or unique indexes), not only in handlers.

## Migrations
- Create migrations with the EF Core CLI only, from the repository root:
  `dotnet ef migrations add <Name> --project Infrastructure --startup-project WebAPI --output-dir Persistence/Migrations`
- Do not hand-write new migrations and do not edit a migration that has already been applied. Change the model, then add a new migration, and check that it preserves existing data.
- After a persistence refactor, check for pending model changes (`dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project WebAPI`).
- Seed data lives in `[Project]Seed`. Changing it requires a new migration.
- Known exceptions to these rules (e.g. an old hand-written migration) are listed in `Requirements.md`. Do not copy their style.
- The database is migrated automatically at startup (`MigrateDatabaseAsync` in `Program.cs`).

## Application Structure
```
Application/
  Common/
    Abstractions/   (ICommandHandler, IQueryHandler, I[Entity]Repository, IUnitOfWork)
    Decorators/     (ValidationDecorator, LoggingDecorator)
    Pagination/     (Pagination, PageQueryValidator)
    Responses/      (every response record: [Entity]Response, ...)
    Results/        (Result, Error, PagedResult)
  [Module]/
    Commands/[UseCaseName]/
    Queries/[UseCaseName]/
  Shared/
    Errors/[Module]Errors.cs, PersistenceErrors.cs
  DependencyInjection.cs
```

## File Naming
- `[UseCaseName]Command.cs` / `[UseCaseName]Query.cs`
- `[UseCaseName]CommandHandler.cs` / `[UseCaseName]QueryHandler.cs`
- `[UseCaseName]CommandValidator.cs` / `[UseCaseName]QueryValidator.cs`
- `[ResponseName]Response.cs`: every response record lives in `Application/Common/Responses/` with the namespace `Application.Common.Responses`, one file per response, whether one or many use cases return it.
- `[UseCaseName]Mapping.cs` only if the use case has 2+ mappings; otherwise map inside the handler.
- `[Entity]Repository.cs` implementing `I[Entity]Repository`.

## Rules
- Commands change state and return `Result<T>` when there is a response payload (e.g. `Result<Guid>` for a created entity ID), or `Result` when there is no payload. Queries are read-only and return `Result<TResponse>`.
- Commands and queries are plain records. No marker interfaces.
- Handlers: `internal sealed`, primary constructor, return `Result` or `Result<T>` according to the command or query contract, always pass `CancellationToken`.
- For expected failures (NotFound, Validation, Conflict), return a failed `Result` or `Result<T>` containing an `Error`.
- Use `ValidationDecorator` to validate commands and queries that have a registered validator. Use a logging decorator for handler execution. Follow the Transactions section for transaction handling.
- DI: `Application` exposes `AddApplication()`, and `Infrastructure` exposes `AddInfrastructure()`. In `AddApplication()`, use Scrutor to register command and query handlers and validators as scoped services. Set `publicOnly: false` in `AddClasses` to include `internal sealed` types, and exclude open generic decorators from the scan.

## Mapping
- Commands: build entities through domain factory methods (`[Entity].Create(...)`), not object initializers. Use a `ToEntity` extension only when the factory call needs non-trivial argument mapping.
- Queries: EF Core projections live in repositories. Use `AsNoTracking()` and project directly to the response with `Select`. Never load the entity and map it in memory.
- When a repository uses the same projection in 2+ methods, define it once as a `private static readonly Expression<Func<[Entity], [Entity]Response>> ToResponse` in that repository and pass it to `Select`.

## Validation
- Every command has a validator.
- Every query that takes an ID or is paginated has a validator. IDs are checked with `NotEmpty()` (route constraint `{id:guid}` still accepts `Guid.Empty`).
- Paginated query validators inherit from `PageQueryValidator<T>`.
- Validators mirror domain constraints using the entity's constants (`[Entity].Maximum[Property]Length`), so invalid user input is rejected before reaching the domain.
- Domain factory methods still guard their invariants with `ArgumentException` / `ArgumentOutOfRangeException`. These only fire on programming errors and end up in the global exception handler as a 500.

## Errors
- Each module has an `internal static class [Module]Errors` in `Application/Shared/Errors`, with `public static readonly Error` fields. Persistence-level errors live in `PersistenceErrors`.
- Error codes use the form `[Module].[Reason]` with a plural module name, e.g. `[Module].NotFound`.
- `ErrorType` maps to HTTP status in `ResultExtensions.ToProblemDetails`: `Validation` → 400, `NotFound` → 404, `Conflict` → 409, `UnprocessableEntity` → 422, `BadGateway` → 502, `GatewayTimeout` → 504. The error code is returned in the `code` extension of the `ProblemDetails`.

## Controllers
- No business logic. Inject handlers per action with `[FromServices]`, convert a failed `Result` with `result.ToProblemDetails(HttpContext)`.
- Request body:
  - When the body maps 1:1 to the command, bind the command record directly (e.g. `Create[Entity]Command`).
  - When the command also needs route values, define a `[Action]Request` record at the bottom of the controller file and build the command in the action.
- Responses:
  - Create command returning an ID → `201 Created` via `CreatedAtAction(nameof(Get), new { id }, id)`.
  - Command without payload → `204 No Content`.
  - Query → `200 OK` with the response.
- Every action has `[EndpointSummary]` and a `[ProducesResponseType]` for the success status and for every error status it can return (`ValidationProblemDetails` for 400, `ProblemDetails` for 404/409).
- List endpoints take `page` and `pageSize` from the query string with defaults from `Pagination`.
- Keep `WebAPI/WebAPI.http` in sync with the endpoints: add an example for every new endpoint and remove examples for deleted ones.
- API documentation, Development only: `MapOpenApi()` serves one OpenAPI document per API version (`/openapi/v1.json`, `/openapi/v2.json`, ...), and `MapScalarApiReference()` (package `Scalar.AspNetCore`) serves an interactive UI at `/scalar` with a version dropdown. Both are generated from the controllers, so `[EndpointSummary]` and `[ProducesResponseType]` are what the UI shows. Do not expose them outside Development.

## API Versioning
- Packages: `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer`. Configured in `Program.cs` with `AddApiVersioning(...).AddMvc().AddApiExplorer(...)`.
- The version is a URL segment: every controller route is `api/v{version:apiVersion}/[module]`, and every request URL contains it (`/api/v1/...`). There is no unversioned route and no default version for requests without one.
- Every controller declares the versions it serves with `[ApiVersion("N.0")]`. A new controller starts at the version where it is introduced.
- All versions of one resource stay in one controller:
  - an action that is the same in every declared version has no version attribute;
  - an action that differs per version exists once per version, each with `[MapToApiVersion("N.0")]`, and the newer one is named `[Action]V[N]` (e.g. `GetV2`). Both keep the same route template.
- Create a new version only for a breaking change (a removed or renamed field, a changed type or meaning, a changed status code). Additive changes (a new optional field, a new endpoint) go into the current version.
- When a newer version replaces an older one, mark the older one `[ApiVersion("N.0", Deprecated = true)]`. `ReportApiVersions = true` returns `api-supported-versions` (active versions only) and `api-deprecated-versions` headers; a deprecated version appears only in the second one. A URL with no version or an unknown version matches no route and returns 404; `app.UseStatusCodePages()` (with `AddProblemDetails()`) gives that and every other empty error response a `ProblemDetails` body. Remove a deprecated version only after announcing it in `Requirements.md`.
- Application is version-agnostic: commands, queries, handlers and responses never mention API versions.
  - A version that only reshapes an existing Application response uses a contract in `WebAPI/Contracts/V[N]/[Entity]V[N]Response.cs` with a `To[V[N]]Response()` mapping extension in the same file, called by the controller.
  - A version that needs different data gets a new query or command in Application, named by what it does, not by the API version.
- `CreatedAtAction` keeps the requested version in the `Location` URL (the `version` route value is ambient), so a create in v2 points to the v2 GET.
- OpenAPI: `AddApiExplorer` uses `GroupNameFormat = "'v'VVV"` and `SubstituteApiVersionInUrl = true`, so each version becomes a group named `v1`, `v2`, ... When adding a version, register its document with `builder.Services.AddOpenApi("vN")` and add it to Scalar with `.AddDocument("vN", ...)` (mark the newest `isDefault: true`).
- `WebAPI.csproj` suppresses the analyzer warnings AV0029/AV0030 (they suggest `Asp.Versioning.OpenApi`, which does not yet support Microsoft.OpenApi 3.x used by .NET 11). Remove the suppression and switch to `AddApiVersioning().AddOpenApi()` + `MapOpenApi().WithDocumentPerVersion()` once a compatible release exists.
- Keep `WebAPI.http` examples on explicit versions, with at least one example per version that differs.

## Idempotency
- Every action that creates something (every POST) is marked `[Idempotent]` (`WebAPI/Idempotency`). GET, PUT and DELETE are idempotent by HTTP semantics and are not marked.
- A marked action **requires** the `Idempotency-Key` header: one value, 1 to 100 visible ASCII characters (clients should send a new UUID per logical operation). Missing → 400 `Idempotency.KeyMissing`; malformed → 400 `Idempotency.KeyInvalid`.
- `IdempotencyFilter` is a resource filter, so it runs before model binding, validation and the handler:
  - it hashes the request (SHA-256 of method, path with version, query and the raw body) and claims the key through `IIdempotencyStore`;
  - same key + same request, already finished → the stored response is replayed byte for byte (status, `Location`, body) with header `Idempotency-Replayed: true`; the handler does not run;
  - same key + different request (other path, version or body, even different whitespace) → 422 `Idempotency.KeyReused`;
  - same key while the first request is still running → 409 `Idempotency.RequestInProgress`;
  - new key → the action runs, and its response is stored.
- Stored: every response below 500, including 4xx business errors (a replayed `Enrollments.AlreadyEnrolled` stays 409). Not stored: 5xx responses and exceptions; the claim is released so the client can retry with the same key.
- `IIdempotencyStore` (Application/Common/Abstractions) is implemented by `IdempotencyStore` in `Infrastructure/Persistence/Idempotency` over the `IdempotencyRecords` table. The table's primary key on `Key` makes the claim atomic, so concurrent requests with one key run the action at most once, even across several instances. The claim is saved and detached immediately, and the result is written with `ExecuteUpdate`, so it never mixes with the handler's Unit of Work.
- Keys expire after `Idempotency:ExpirationHours` (appsettings, default 24). An expired key is removed when it is used again; there is no background cleanup yet.
- Handlers know nothing about idempotency; never pass the key into a command.
- The `Idempotency-Key` header is documented in OpenAPI/Scalar by `IdempotencyKeyOperationTransformer`, registered on every `AddOpenApi("vN")` document: a required header parameter (`format: uuid`) with an **empty** `example`, so Scalar and client generators add the header to every such request with no value and the caller only fills it in. Never put a real key as the example: every caller would send the same key and get replays or 422. Every `[Idempotent]` action also declares 409 and 422 with `[ProducesResponseType]`.
- Every POST example in `WebAPI.http` carries its own `Idempotency-Key`. Re-running an example within the expiration window replays its first response.

## Rate Limiting
- ASP.NET Core built-in rate limiting (`Microsoft.AspNetCore.RateLimiting`, no package), configured in `WebAPI/RateLimiting/RateLimitingSetup.cs` by `AddApiRateLimiting(configuration)` and enabled with `app.UseRateLimiter()`.
- Every limit is per client IP (`Connection.RemoteIpAddress`). Behind a reverse proxy, configure `ForwardedHeaders` first, otherwise all clients share the proxy's IP. When authentication is added, partition by user id instead.
- Three layers; a request must pass the global limiter and its endpoint policy:
  - **Global limiter** on every request: fixed window (`RateLimiting:Global`).
  - **`RateLimitPolicies.Writes`** on every action that changes data (every POST and DELETE): sliding window (`RateLimiting:Writes`). Add `[EnableRateLimiting(RateLimitPolicies.Writes)]` to every new POST, PUT, PATCH or DELETE action.
  - **`RateLimitPolicies.ExternalServices`** on every external-service controller (class-level `[EnableRateLimiting(...)]`): token bucket (`RateLimiting:ExternalServices`), so a short burst is allowed and then a steady rate protects the external API. Its POST uses this policy, not `Writes` (an endpoint has one named policy; the action-level attribute would override the class-level one).
- All numbers live in the `RateLimiting` section of `appsettings*.json`; never hard-code limits. `QueueLimit` is 0: excess requests are rejected at once, never queued.
- A rejected request returns 429 `ProblemDetails` with code `RateLimit.Exceeded`, `correlationId`, and a `Retry-After` header (seconds). It is logged as a Warning with the ProblemDetails JSON. It never reaches MVC: no model binding, no idempotency key is claimed, no handler runs.
- Not rate limited (`.DisableRateLimiting()`): `/health` (monitoring must always reach it) and the Development-only `/openapi/*` and `/scalar`.
- `UseRateLimiter()` runs after `CorrelationIdMiddleware`, request logging, `UseExceptionHandler()` and `UseStatusCodePages()`, and before the controllers.
- The 429 response is documented on every operation by `TooManyRequestsOperationTransformer`, registered on every `AddOpenApi("vN")` document, so actions do not repeat `[ProducesResponseType(429)]`.
- Limiters are in memory: with several instances, each instance counts separately. A shared limit across instances needs a distributed store (e.g. Redis).

## Caching
- ASP.NET Core built-in Output Caching (`Microsoft.AspNetCore.OutputCaching`, no package), configured in `WebAPI/Caching/CachingSetup.cs` by `AddApiCaching(configuration)` and enabled with `app.UseOutputCache()` right after `app.UseRateLimiter()` (a cache hit still counts toward the rate limits). A hit is served before MVC: no handler, no database, no external call.
- Caching is a WebAPI concern. Application knows nothing about it: no cache calls in handlers, no caching decorator, no marker interfaces on queries.
- Every GET action is marked with `[OutputCache(PolicyName = CachePolicies.X, Tags = [CacheTags.Y])]`:
  - `CachePolicies.LocalData`: the application's own data; expires after `Caching:LocalDataSeconds` and is evicted earlier by writes.
  - `CachePolicies.ExternalServices`: GETs of external-service controllers; expires after `Caching:ExternalServicesSeconds`, never evicted by our writes (we do not own the data).
  - Tags (`CacheTags`) name the data a response shows, one per module (`[module]`), and one per external service.
- Every write action (POST/PUT/PATCH/DELETE) that changes local data is marked `[InvalidatesCache(CacheTags.X, ...)]` with the tag of **every** module whose cached responses it changes, including side effects (e.g. a write that changes a counter shown by another module evicts that module's tag too). The filter evicts the tags only when the action returns 2xx, before the response is written. An idempotent replay does not run the action and evicts nothing (nothing changed).
- The cache key is the full URL (version, path, query string), so `?page=2` and `/api/v2/...` are cached separately. Only 200 responses of GET/HEAD are stored; errors, writes and requests with `Authorization` or cookies are not. `/health` is never cached.
- When authentication is added, a response that depends on the user must vary by user (or not be cached); the default policy skips authenticated requests.
- All durations live in the `Caching` section of `appsettings*.json`; never hard-code them.
- The store is in memory, per instance: with several instances, an eviction on one does not reach the others. A shared cache needs a distributed store (e.g. `Microsoft.AspNetCore.OutputCaching.StackExchangeRedis`); endpoints do not change.

## External Services
Placeholder: `[Service]` (external API name, e.g. the provider's product name).
- Application owns the contract: `I[Service]Client` in `Application/Common/Abstractions`, returning `Result<T>` with response records from `Application/Common/Responses`. Application never references `HttpClient`, HTTP status codes or the external JSON shapes.
- Each external service is one Application module named after the service, not one module per external resource:
  - use cases in `Application/[Service]/Commands/[UseCaseName]/` and `Application/[Service]/Queries/[UseCaseName]/`, named by the resource (e.g. `ListUsers`, `GetUser`, `CreatePost`);
  - responses `[Service][Resource]Response` in `Application/Common/Responses/` (e.g. `[Service]UserResponse`);
  - the service's own errors in `Application/Shared/Errors/[Service]Errors.cs`, with codes `[Service].[Reason]` (e.g. `[Service].UserNotFound`).
- Infrastructure owns the implementation in `Infrastructure/ExternalServices/[Service]/`:
  - `ExternalServicesOptions` (`Infrastructure/ExternalServices/`): settings shared by all external services, bound from the `ExternalServices` section. It holds one `TimeoutSeconds` used by every client; it is set in configuration and not validated in code. Do not add a per-service timeout.
  - `[Service]Options`: the service's own settings (its `BaseUrl`), bound from `ExternalServices:[Service]` with `BindConfiguration`. `BaseUrl` is written in configuration as an absolute URL ending with `/`; it is not validated or normalized in code. Never hard-code URLs or timeouts.
  - `[Service]Models.cs`: `internal` records matching the external JSON. They never leave Infrastructure; the client maps them manually to response records.
  - `[Service]Client`: `internal sealed` typed HttpClient registered with `AddHttpClient<I[Service]Client, [Service]Client>` in `AddInfrastructure()`, which sets `BaseAddress = new Uri([Service]Options.BaseUrl)` and `Timeout` from `ExternalServicesOptions.TimeoutSeconds`.
- The client translates every expected failure in one private `SendAsync` helper and never throws for them:
  - HTTP 404 → `ExternalServiceErrors.NotFound`
  - any other non-success status or `HttpRequestException` → `ExternalServiceErrors.Unavailable` (502)
  - `OperationCanceledException` while the caller's token is not cancelled (the timeout elapsed) → `ExternalServiceErrors.Timeout` (504)
  - `JsonException`, an empty body, or a missing expected header → `ExternalServiceErrors.InvalidResponse` (502)
  - a cancellation requested by the caller is rethrown.
  Each failure is logged as a Warning with method and URL.
- Handlers translate `ExternalServiceErrors.NotFound` to the service's own error (e.g. `[Service]Errors.UserNotFound`) and return other errors as they are.
- External list endpoints still return `PagedResult<T>`: use the external API's paging parameters and total-count header when it has them; do not fetch everything and page in memory.
- No automatic retries by default. Never retry non-idempotent requests (POST) automatically.
- External services are not added to `/health`: their outage does not make this application unhealthy.
- One controller per external service: `[Service]Controller` with the route prefix `api/v{version:apiVersion}/[service]` (lowercase service name), holding all of that service's endpoints (e.g. `api/v1/[service]/[resource]`). Do not split a service's endpoints across controllers per resource.
- A create call to an external API with no local read endpoint returns `201` via `Created((string?)null, value)`.

## Cross-Cutting Rules

**Global error handling**
- Unexpected exceptions are handled by a single `IExceptionHandler` registered in WebAPI that returns a uniform `ProblemDetails`.
- Never return stack traces or exception messages to the client. Log the exception server-side.
- Every failed response is logged with the exact `ProblemDetails` JSON sent to the client (`Responded {StatusCode} with {ProblemDetails}`): `ProblemDetailsLoggingFilter`, a global result filter, logs it for controller results (Warning for 4xx, Error for 5xx), and `GlobalExceptionHandler` logs it for unexpected exceptions. Successful responses are not logged this way.

**Transactions**
- One `IUnitOfWork.SaveChangesAsync` per command is the default and is already atomic.
- If a command needs multiple saves that must succeed together, expose an explicit transaction through an Application interface. No global transaction decorator.

**Concurrency**
- Use a concurrency token (`RowVersion`, a `Guid` regenerated by the entity on every change) on entities that support concurrent edits.
- Include `rowVersion` in read responses and require it in update requests.
- Use the client-supplied `rowVersion` as the expected version when saving the update.
- Return a `Conflict` error (HTTP 409) if the version is stale or the Unit of Work returns `PersistenceErrors.ConcurrencyConflict`.
- An operation that changes a counter or state on another entity as a side effect does not take a client `rowVersion`, but still maps `PersistenceErrors.ConcurrencyConflict` to a module `Conflict` error.

**Pagination and filtering**
- Every list query returns `Result<PagedResult<T>>` (items, page, pageSize, totalCount). Repositories build it with `ToPageAsync` from `Infrastructure/Persistence/PagedQueryExtensions.cs`.
- Page size has a default (`Pagination.DefaultPageSize`) and a hard maximum (`Pagination.MaximumPageSize`) enforced in the query validator.
- Every list query has a deterministic order: a business field first, then `Id` as a tie-breaker.
- Client-side filtering and sorting use one shared pattern (contract in `Application/Common/Pagination`, applied in Infrastructure). If it does not exist yet, design it, document it here, and only then use it. Do not add ad-hoc filter parameters to a single endpoint.

**Domain entities**
- Entities are not anemic: business rules live inside them (`[Entity].Create(...)`, `entity.Cancel()`), with private setters and a private parameterless constructor for EF Core.
- Handlers orchestrate (load through repositories, call domain methods, save through `IUnitOfWork`). They do not contain business rules.
- Checks that need the database (an entity exists, a duplicate exists) are done in the handler through repository methods before calling the domain method, and return the matching `Error`.

**Observability**
- Use **Serilog** for structured logging (inject `ILogger<T>`, no static `Log` calls in application code) and Serilog request logging for HTTP.
- Serilog sinks and minimum levels are configured only in the `Serilog` section of `appsettings*.json` (read with `ReadFrom.Configuration`), never in code. Logs go to the console and to daily rolling, human-readable text files `logs/log-yyyyMMdd.txt` under the app's working directory, kept for 14 days. The `logs/` folder is git-ignored. The file uses a `Serilog.Expressions` template that draws a separator block before every event whose `EventId.Name` is `RequestStarted`; `CorrelationIdMiddleware` logs that event (method, path, query string, correlation id) at the start of each request. Do not log other events with that EventId. To trace a request, search the files for its `CorrelationId` and read down from its separator. Lines from concurrent requests can interleave.
- EF Core sensitive data logging (SQL parameter values in logs and in EF exception messages) is switched by `Database:EnableSensitiveDataLogging` in `appsettings*.json` and passed to `AddInfrastructure()`. Never hard-code it. Where it is enabled, treat log files as containing personal data. The value each project uses is recorded in `Requirements.md`.
- Every request has a correlation id set by `CorrelationIdMiddleware` from `HttpContext.TraceIdentifier`. It is server-generated (an incoming header is not trusted), returned in the `X-Correlation-Id` response header, added to the log scope, and included as `correlationId` in every `ProblemDetails`.
- Expose `/health` with a database readiness check from the start.

## Testing
- If the solution has a test project, add or update tests for every changed use case and run them.
- Always verify changes by building the solution file (`*.slnx` / `*.sln`) at the repository root with `dotnet build` and by running the affected requests in `WebAPI/WebAPI.http`, including the failure cases.

## New Use Case Checklist
1. Check `Requirements.md` for the business rules; add the new rule there if it is missing.
2. Create the folder under `[Module]/Commands` or `[Module]/Queries`.
3. Add the command or query record, its handler, and its validator (see Validation). Add a response record where needed.
4. Add or reuse repository methods (see Data Access). A new repository is registered automatically when it is named `[Entity]Repository` and implements `I[Entity]Repository`.
5. Add errors to `[Module]Errors.cs` if needed.
6. If the model changed, add a migration with the EF Core CLI (see Migrations).
7. Add the controller action following the Controllers, API Versioning, Idempotency, Rate Limiting and Caching sections (a POST gets `[Idempotent]`; a POST/PUT/PATCH/DELETE gets `[EnableRateLimiting(RateLimitPolicies.Writes)]` and `[InvalidatesCache(...)]` with every affected tag; a GET gets `[OutputCache(...)]` with its module's tag), and an example request with an explicit version (and an `Idempotency-Key` for a POST) in `WebAPI.http`.
8. Add the endpoint to `Requirements.md`.
9. Run `dotnet build` on the solution file at the repository root.
