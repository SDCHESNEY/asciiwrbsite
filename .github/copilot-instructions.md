---
description: 'Guidance for Copilot when editing .NET 9/C# Blazor web apps in this repo'
applyTo: '**/*.cs, **/*.razor, **/*.razor.cs, **/*.csproj, appsettings*.json'
---

# Blazor Web App Copilot Instructions

Ground every suggestion in GitHub's `github/awesome-copilot` instructions for `blazor`, `csharp`, `dotnet-architecture-good-practices`, and `security-and-owasp`. Default to production-ready code; create illustrative scaffolds only when explicitly requested.

## Project Context & Baseline
- Target .NET 9 SDK with nullable reference types enabled, global usings, and file-scoped namespaces; opt into C# 14 features only if the project already does so, otherwise stay on the latest stable (C# 13).
- Prefer Razor component projects (`.razor` plus optional `.razor.cs`) with supporting server APIs; keep Program.cs minimal, delegating configuration to extension methods.
- Keep configuration layered: `appsettings.json` for defaults, environment files for overrides, and secrets in user secrets/Azure Key Vault—never hardcode credentials.

## Architecture & Layering
- Follow the DDD/SOLID workflow from the `dotnet-architecture` instruction set: before coding, summarize which aggregates, domain events, or services change and how security/compliance is preserved.
- Enforce clear separation among Domain (entities, value objects, rules), Application (commands/queries, DTOs), Infrastructure (EF Core, HTTP, storage), and UI (Blazor components).
- Constructor-inject abstractions, not concretes; expose interfaces from the domain/application layers and implement them in Infrastructure.
- Record important decisions inline or in README-style docs to preserve ubiquitous language.

## Solution Structure & Development Flow
- Organize the solution into clear projects (e.g., `Client`, `Server`, `Shared`) or bounded contexts; inside projects prefer feature folders (`Features/Orders`, `Features/Inventory`) that keep components, services, and tests collocated.
- Keep Program.cs slim: register services via extension methods (`builder.Services.AddDomainServices()`) and move pipeline configuration to dedicated static classes.
- Use layout components for global chrome, route data for navigation, and CSS isolation (`Component.razor.css`) to scope styling.
- Centralize cross-cutting services (logging, caching, localization) via DI registrars so components only reference abstractions defined in the UI or application layers.
- Treat `Shared` models as contracts; update Swagger/OpenAPI and XML docs whenever they change, and version APIs when breaking changes are unavoidable.

## Coding Standards & Naming
- PascalCase for types, public members, Razor components, and `.razor` files; camelCase for locals and private fields; prefix interfaces with `I`.
- Keep braces on new lines, use expression-bodied members when obvious, and favor pattern matching / `switch` expressions for branching.
- Use `nameof`, target-typed `new()`, and `readonly record struct` for value objects.
- All public APIs get XML docs; include `<example>` blocks for shared components or services.

## Blazor Component Craftsmanship
- Use lifecycle hooks (`OnInitializedAsync`, `OnParametersSetAsync`) for async setup; avoid synchronous blocking.
- Keep markup self-contained and move complex logic into `.razor.cs` partial classes or injected services.
- Bind data with `@bind-Value` + `EventCallback` to avoid manual state sync; call `StateHasChanged` sparingly and override `ShouldRender` for expensive components.
- Wrap risky UI in `<ErrorBoundary>` and log failures via injected `ILogger<TComponent>`.

### Good Example – Stateless form component
```razor
@using Contoso.Client.Features.Tickets
@inject TicketClient Client

<EditForm Model="_request" OnValidSubmit="HandleSubmit">
    <InputText @bind-Value="_request.Subject" />
    <button class="btn btn-primary" disabled="@_submitting">Create</button>
</EditForm>

@code {
    private readonly TicketRequest _request = new();
    private bool _submitting;

    private async Task HandleSubmit()
    {
        _submitting = true;
        await Client.CreateAsync(_request);
        _submitting = false;
    }
}
```
Avoid storing mutable global state in static fields or bypassing DI.

## State, Data & API Integration
- Use typed `HttpClient` registrations via `IHttpClientFactory`; always pass a `CancellationToken`.
- Cache hot API responses with `IMemoryCache` (server) or `Blazored.LocalStorage` / `sessionStorage` (WASM). For multi-node deployments, prefer Redis via `IDistributedCache`.
- Adopt Cascading Parameters for light-weight state, and escalate to Fluxor/BlazorState when the component tree becomes complex.
- For data access, use EF Core with repository or specification patterns only when they add clarity; keep queries async, paginated, and projection-based to reduce payloads.

## Security & Compliance
- Enforce least privilege and “deny by default” authorization attributes/policies; use ASP.NET Identity or JWT Bearer auth for Blazor WASM.
- Parameterize every query, sanitize user-provided URIs (protect against SSRF), and never deserialize arbitrary payloads without validation.
- Store secrets outside code, require HTTPS, configure strict CORS, and add security headers (CSP, HSTS, X-Content-Type-Options) where middleware exposes responses.
- Log authentication/authorization failures with correlation IDs, and surface `ProblemDetails` payloads instead of raw exceptions.

## Performance & Observability
- Keep components lean: split large Razor files, avoid re-render storms, and offload CPU-heavy work to background services or `IHostedService`.
- Use async all the way, stream data where possible, and coalesce state updates before triggering UI refreshes.
- Instrument with structured logging (Serilog or ILogger scopes) and Application Insights; include trace identifiers in API responses when debugging.
- Profile regularly with Visual Studio diagnostics; add caching and compression middleware once bottlenecks are measured.

## Testing & Validation
- Prefer xUnit + bUnit for component tests, plus integration tests hitting minimal APIs; mocking via Moq/NSubstitute only when DI boundaries demand it.
- Follow the `MethodName_Condition_ExpectedResult` naming style and omit “Arrange/Act/Assert” comments, matching the `csharp` instruction set.
- Cover the full testing pyramid:
    - **Unit**: value objects, services, and component logic using deterministic inputs.
    - **Component/UI**: bUnit to verify rendering, bindings, and event callbacks with minimal DOM assumptions.
    - **Integration**: `WebApplicationFactory` to exercise EF Core, HttpClient, authentication, and routing; assert `ProblemDetails` on failures.
    - **End-to-end**: Playwright (if configured) for critical user journeys; run headless in CI.
- Reuse deterministic builders/fakes for test data; avoid hitting external services by default, but document any integration tests that require them.
- Always run `dotnet test --configuration Release` (or the solution’s `Test` pipeline command) before opening a PR; include code coverage reports when available.

### Good Example – Component logic test
```csharp
[Fact(DisplayName = "HandleSubmit_Dispatches_CreateRequest")]
public async Task HandleSubmit_Dispatches_CreateRequest()
{
    // setup
    var client = Substitute.For<ITicketClient>();
    var component = new TicketForm(client);

    // act
    await component.InvokeSubmitAsync();

    // assert
    await client.Received(1).CreateAsync(Arg.Any<TicketRequest>(), Arg.Any<CancellationToken>());
}
```
Add integration tests for authentication, authorization, and failure paths; use `WebApplicationFactory` for server APIs.

## DevOps & Delivery
- Run `dotnet restore`, `dotnet build`, `dotnet test`, and `dotnet format` before pushing; fail fast on warnings-as-errors where configured.
- Containerize via `dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer` or an explicit Dockerfile, and include health probes for hosted APIs.
- Keep Swagger/OpenAPI documents current; generate clients if the Blazor app consumes internal APIs.
- When wiring CI/CD, gate deployments on automated tests and security scans (e.g., `dotnet list package --vulnerable`).

### Docker & Production Deployment
- Use multi-stage Dockerfiles: build/publish in `mcr.microsoft.com/dotnet/sdk:9.0` and run on `mcr.microsoft.com/dotnet/aspnet:9.0` (Alpine only if globalization needs are addressed). Copy only the published output and prune dev-time artifacts.
- Set `ASPNETCORE_ENVIRONMENT=Production` inside the container, load secrets via environment variables or mounted providers, and never bake secrets into images.
- Expose the port expected by the hosting platform (`ASPNETCORE_URLS=http://+:8080` commonly) and add container-level health checks hitting `/health` or a minimal API endpoint.
- Run as a non-root user (`USER app`) and keep file permissions minimal; rely on `dotnet monitor` or OpenTelemetry exporters for diagnostics rather than SSHing into containers.
- Parameterize configuration with `appsettings.{Environment}.json` plus `--environment` overrides; prefer `DOTNET_gcServer=1` and `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` only when culture data is baked in.
- For multi-container deployments, provide a `docker-compose.yml` or Helm chart snippet explaining how to wire Redis/SQL dependencies, ensuring networks/policies enforce least privilege between services.

## Verification Checklist
- ✅ Domain design: aggregates, value objects, and events align with ubiquitous language and SOLID.
- ✅ Implementation quality: DI boundaries respected, async flows complete, and ProblemDetails emitted for errors.
- ✅ Security: OWASP Top 10 mitigations applied (authZ, SSRF controls, secret handling, headers).
- ✅ Testing: unit/bUnit/integration coverage includes happy paths and edge cases, following the naming convention.
- ✅ Documentation: Swagger, XML docs, and README snippets updated when contracts or behaviors change.
