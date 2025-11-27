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

- **Phase 0 – Foundations & Tooling (complete):** analyzers, shared props, HTTPS/HSTS/CSP, health endpoint, and green `dotnet test --configuration Release` runs in CI.
- **Phase 1 – ASCII Core & Configuration (complete):** `AsciiArtOptions`, provider services, Hero & About components, and curl/plaintext delivery (`/text`, `Accept: text/plain`).
- **Phase 2 – Blog Platform (complete):** markdown-driven posts (`content/blog/{slug}.md`), caching, YAML frontmatter validation, `BlogIndex`/`BlogPost` components, RSS feed, and curl summaries.
- **Phase 3 – GitHub Showcase (complete):** typed `HttpClient` integrations, ASCII-styled repo cards, filters, and curl-safe fallbacks.
- **Phase 4 – Observability & Delivery (complete):** structured logging with correlation IDs, `/metrics`, response compression/caching, Docker multi-stage builds, and Azure/GCP deployment scripts.
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
- Both paths stream the hero banner, tagline, navigation list, the latest blog summaries, and an About summary, making the site usable from terminals, CI health checks, and automation scripts.

### Editing About content
- Markdown lives in `content/about.md`; edit it to change both the Blazor About page and the curl/plaintext summary.
- The first paragraph becomes the summary shown in curl output, so keep it concise and non-HTML when possible.
- Hot reload picks up file changes while the server runs, so local authors can iterate without restarting.

## Phase 2 Usage Guide

### Authoring blog posts
- Create markdown files under `content/blog/{slug}.md` with the following frontmatter:

```yaml
---
title: Launching ASCII Site
slug: launching-ascii-site
published: 2025-11-01
summary: A one-paragraph teaser displayed in listings and curl output.
tags:
  - ascii
  - roadmap
---
```

- The body markdown is rendered in both Blazor (`/blog/{slug}`) and curl mode. Summaries fall back to the first paragraph when the frontmatter value is omitted.
- Files are parsed once and cached. When `DOTNET_ENVIRONMENT=Development`, file watchers invalidate the cache automatically so hot reload reflects new posts.

### Blog UI and RSS feed
- `/blog` lists posts with ASCII-styled cards, tag filters, and pagination (5 posts per page). `/blog/{slug}` renders the full article.
- `/feed` emits an RSS 2.0 document backed by the same markdown files. CI or feed readers can follow the project without scraping HTML.
- `/text` includes the hero, navigation, About summary, and the latest three blog summaries (title, publish date, and permalink) for parity with terminal-first workflows.

## Phase 3 Usage Guide

### Configure the GitHub showcase
- Populate the `GitHub` section in `appsettings*.json` (client + server) with fallback repositories. Each entry supports `Owner`, `Name`, `DisplayName`, `Description`, `Language`, `Topics`, `Stars`, and `Url`. Tokens for live API calls belong in user secrets or Key Vault only.
- GitHub data flows through `IGitHubRepoService`, which caches responses (`CacheDurationMinutes`) and automatically falls back to the configured metadata whenever the API rate-limits or fails.

```json
"GitHub": {
	"EnableLiveUpdates": true,
	"CacheDurationMinutes": 30,
	"Token": "{use secrets}",
	"Repositories": [
		{
			"Owner": "SDCHESNEY",
			"Name": "asciiwrbsite",
			"DisplayName": "ASCII Site",
			"Description": "Blazor + minimal API playground for ASCII storytelling.",
			"Language": "C#",
			"Topics": ["blazor", "aspnetcore", "ascii"],
			"Stars": 0
		}
	]
}
```

### UI and curl experiences
- Visit `/github` to see ASCII-bordered repo cards with live/fallback metadata, sortable by star count and filterable via language/topic dropdowns.
- The navigation menu and home page link directly to the showcase, keeping discovery simple for browser users.
- `/text` (and `Accept: text/plain` on `/`) now includes a `GITHUB` section that lists up to four repositories with links, topics, and wrapped descriptions so curl users receive the same insight as the UI.

## Phase 4 Usage Guide

### Logging, telemetry, and metrics
- Structured logging is enabled via `AddJsonConsole` with scopes + Activity tracking. Correlate any request by sending `X-Correlation-Id` (GUID). The middleware will reuse caller-supplied IDs or generate new ones and emit them in every response.
- Configure Azure Application Insights by setting `ApplicationInsights:ConnectionString` in `appsettings.Production.json`, environment variables, or Azure Key Vault secrets. When omitted, no telemetry is sent.
- Scrape `/metrics` to gather Prometheus-style counters for total requests plus `/text`, `/feed`, and `/github` visits. The endpoint returns `text/plain` and is safe for unauthenticated scraping.

### Performance & security
- Response compression (gzip + Brotli) is enabled for HTTPS traffic, and `/text` + `/feed` responses now emit cache headers (`Cache-Control: public, max-age=30/300` and `Vary: Accept`).
- Additional headers (`Permissions-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`) lock down browsers. Host sanitization prevents SSRF when generating RSS URLs.

### Containers & deployment
- Use the provided multi-stage `Dockerfile` to build minimal runtime images (`docker build -t ascii-site:dev .`).
- `docker-compose.yml` runs the container locally with health checks and environment variable plumbing.
- See `docs/deploy.md` plus the helper scripts in `deploy/` (`azure-container-apps.sh`, `gcp-cloud-run.sh`) for production-ready deployment flows.

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
