# ASCII Site

A .NET 9 solution that hosts a Blazor experience plus hardened ASP.NET Core APIs for ASCII-themed storytelling. The repo follows the guidance in `.github/copilot-instructions.md` to stay production-ready from the first commit.

## Solution Layout
- `src/AsciiSite.Client` – Blazor client (UI + routing) referencing shared contracts.
- `src/AsciiSite.Server` – Minimal API host that exposes health, plaintext, and future content endpoints with security headers and ProblemDetails enabled.
- `src/AsciiSite.Shared` – DTOs, options, and validation logic used across layers.
- `tests/AsciiSite.Tests` – xUnit test suite with FluentAssertions, bUnit, and NSubstitute.

Shared compiler settings (nullable, analyzers, deterministic builds) live in `Directory.Build.props` so all projects inherit the same baseline.

## Roadmap Snapshot
Each increment follows the detailed plan in `docs/roadmap.md`. Highlights:

- **Phase 0 – Foundations & Tooling (current):** enforce analyzers, shared props, HTTPS/HSTS/CSP, health endpoint, and green `dotnet test --configuration Release` runs in CI.
- **Phase 1 – ASCII Core & Configuration:** add `AsciiArtOptions`, provider services, Hero & About components, and curl/plaintext delivery (`/text`, `Accept: text/plain`).
- **Phase 2 – Blog Platform:** markdown-driven posts (`content/blog/{slug}.md`), caching, ProblemDetails on validation, `BlogIndex`/`BlogPost` components, RSS feed, and curl summaries.
- **Phase 3 – GitHub Showcase:** typed `HttpClient` integrations, ASCII-styled repo cards, and curl-safe fallbacks.
- **Phase 4 – Observability & Delivery:** structured logging, metrics, Docker multi-stage builds, Azure/GCP deployment scripts, and CI publishing workflows.
- **Phase 5 – Future Enhancements:** admin tooling, localization, ASCII animations, headless CMS integrations.

Acceptance criteria per phase always include updated documentation, passing format/build/test/vulnerability scans, and security reviews.

## Development
```bash
# Restore tools and packages
dotnet restore AsciiSite.sln

# Format, build, and run the test suite
dotnet format
DOTNET_ENVIRONMENT=Development dotnet build AsciiSite.sln
dotnet test AsciiSite.sln --configuration Release

# Vulnerability scan
 dotnet list AsciiSite.sln package --vulnerable
```

The server currently exposes `GET /health` for readiness checks and sets strict security headers (HSTS, CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy). Future phases will light up the ASCII hero, blog, GitHub showcase, and curl-friendly rendering as described in `docs/roadmap.md`.
