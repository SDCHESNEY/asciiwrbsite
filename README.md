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

## Phase 1 Usage Guide

### Configure the ASCII hero
- Server and client both bind `AsciiArtOptions` from `appsettings*.json`. Override only the values you need; anything omitted falls back to `AsciiArtDefaults` in `src/AsciiSite.Shared`.
- To replace the banner, add a `HeroLines` array (max 20 entries, 80 chars each) to `src/AsciiSite.Server/appsettings.json` and mirror it in the client if you want design-time parity:

```json
"AsciiArt": {
	"HeroLines": [
		"  __  ___  ",
		" /  |/  /  ",
		"/ /|_/ /   "
	],
	"Tagline": "ASCII-first storytelling for both browsers and curl.",
	"CallToActionText": "Explore the roadmap",
	"CallToActionUrl": "/docs/roadmap"
}
```
- Validation enforces the limits documented above; `dotnet test` fails fast if the JSON violates them.

### Plaintext / curl mode
- Run the server (`dotnet run --project src/AsciiSite.Server`) and hit either of the following:
	- `curl -H "Accept: text/plain" http://localhost:5080/`
	- `curl http://localhost:5080/text`
- Both paths stream the hero banner, tagline, navigation list, and an About summary, making the site usable from terminals, CI health checks, and automation scripts.

### Editing About content
- Markdown lives in `content/about.md`; edit it to change both the Blazor About page and the curl/plaintext summary.
- The first paragraph becomes the summary shown in curl output, so keep it concise and non-HTML when possible.
- Hot reload picks up file changes while the server runs, so local authors can iterate without restarting.

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
