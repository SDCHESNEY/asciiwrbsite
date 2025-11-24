# ASCII Website – Architecture & Delivery Plan

This document applies the guidance in `.github/copilot-instructions.md` to scope a production-ready ASCII-themed site built with .NET 9, ASP.NET Core, and Blazor. The site must render cleanly in browsers and via `curl`, expose configurable ASCII art, publish blog content, list GitHub repositories, and offer an About experience. Delivery targets containerized deployment to Azure or GCP.

## 1. Product Goals
- Deliver a nostalgic ASCII aesthetic without sacrificing accessibility or responsiveness.
- Support both graphical browsers and terminal users (`curl https://ascii.example.com`).
- Allow non-engineers to update ASCII art, blog posts, and GitHub links through configuration or markdown content.
- Adhere to the repo’s Copilot instructions for architecture, security, testing, and DevOps readiness.

## 2. Core Experiences
| Area | Requirements |
| --- | --- |
| **Landing / Hero** | Render configurable ASCII banner plus tagline; include quick links to Blog, GitHub, About. |
| **ASCII Art Feed** | Pull art definitions from `appsettings.(Environment).json` (e.g., `AsciiContent:PrimaryBanner`). Provide fallback art when config missing. |
| **Blog** | Support markdown posts (frontmatter: title, date, slug, tags). Generate Blazor routes per post and an index with pagination. Optional RSS/Atom feed. |
| **GitHub Showcase** | Read repo metadata from config or GitHub API; list name, description, language, star count, and link. |
| **About Page** | Markdown-driven bio, timeline, call-to-action buttons. |
| **cURL Mode** | `GET /text` (or root with `Accept: text/plain`) returns plain-text layout (banner, nav, latest blog summaries, repo links). |

## 3. Architecture Overview
- **Solution Layout**
  - `AsciiSite.Client` (Blazor Server or WASM + ASP.NET Core host) – UI components, routing, state.
  - `AsciiSite.Server` (if split) – Minimal APIs for blog feeds, repo aggregation, health checks.
  - `AsciiSite.Shared` – DTOs, options records, validation.
- **Layering** (per instructions): Domain (content entities, value objects), Application (services, queries), Infrastructure (file store, GitHub integration), UI (Blazor components, Razor pages for text mode).
- **Configuration Pipeline**: bind strongly typed options (`AsciiOptions`, `BlogOptions`, `GitHubOptions`) with validation in Program.cs extension methods.
- **Storage Choices**: start with local markdown/json files; enable swap to blob storage without touching UI.

## 4. Blazor Component Strategy
- Layouts: `MainLayout` (standard), `PlainTextLayout` (cURL), `AdminLayout` (future CMS).
- Feature folders: `Features/Ascii`, `Features/Blog`, `Features/GitHub`, `Features/About` with `.razor`, `.razor.cs`, services, and tests colocated.
- Use Cascading Parameters for theme + ASCII palette; prefer scoped services for state (e.g., `SiteContentStore`).

## 5. Configurable ASCII Art
- Define ASCII strings or arrays under `AsciiArt` section:
  ```json
  "AsciiArt": {
    "PrimaryBanner": "__  ___  ___  _ __  _ \n\\ \\ / _ \\ ...",
    "Footer": ["/\\_/\\", "( o.o )", " > ^ <"]
  }
  ```
- Provide `IOptionsSnapshot<AsciiArtOptions>` for runtime updates without restart (when using Azure App Configuration / GCP Secret Manager).
- Expose admin CLI or future UI to edit config; for now, document how to update environment-specific files.

## 6. Blog & Content Workflow
- Posts live in `content/blog/{slug}.md` with YAML frontmatter.
- Build-time pipeline: parse markdown via `Markdig`, cache in memory, watch for file changes in Development.
- Provide Application service `BlogPostProvider` with filtering (tags, year) and search-friendly metadata.
- Add `ProblemDetails` responses for missing posts.

## 7. GitHub Repository Listing
- Options-driven list for deterministic rendering; optionally hydrate with GitHub REST API using typed `HttpClient`.
- Cache responses (IMemoryCache) and decorate with `ETag` to reduce rate-limit usage.
- Render cards in ASCII style (bordered monospace) with CTA buttons linking to GitHub.

## 8. Curl-Friendly Delivery
- Middleware inspects `User-Agent` or `Accept: text/plain`.
- When triggered, bypass Blazor circuit and return pre-rendered plain text using Razor Pages or minimal API that composes ASCII banner + textual sections.
- Ensure tests cover plaintext formatting (line widths, newline normalization).

## 9. Testing Plan
- **Unit**: options validators, markdown parsing, ASCII formatting helpers.
- **Component**: bUnit tests for hero, blog list, repo list, ensuring proper bindings and fallback art.
- **Integration**: `WebApplicationFactory` to verify `/text`, `/blog/{slug}`, `/healthz`, configuration binding, and ProblemDetails responses.
- **E2E**: Optional Playwright to confirm navigation, light/dark ASCII themes, and responsive layout.
- Naming pattern `MethodName_Condition_ExpectedResult`. Run `dotnet test --configuration Release` in CI.

## 10. Security & Compliance Notes
- Deny-by-default authorization; even if public now, prep for future admin routes.
- Sanitize markdown output, enforce HTTPS, set security headers (CSP, HSTS, X-Content-Type-Options).
- Keep secrets (GitHub tokens, storage keys) in Azure Key Vault or GCP Secret Manager; use managed identities where possible.

## 11. Deployment & Docker
- Multi-stage Dockerfile: build on `mcr.microsoft.com/dotnet/sdk:9.0`, publish to `mcr.microsoft.com/dotnet/aspnet:9.0` runtime.
- Include health endpoint `/healthz` used by Azure App Service, Azure Container Apps, or GCP Cloud Run/GKE.
- Configure environment variables for ASCII art overrides, GitHub tokens, and blog storage paths.
- For Azure: provide Bicep/ARM or container app YAML referencing ACR image. For GCP: furnish Cloud Run deploy instructions (set `--port=8080`, `--allow-unauthenticated`).
- Log to stdout/stderr with structured JSON; wire Application Insights or Cloud Logging exporters via `ILogger` providers.

## 12. Developer Workflow
1. Update content/config.
2. Run `dotnet format`, `dotnet build`, `dotnet test`.
3. Execute local docker build `docker build -t ascii-site .` and run `docker run -p 8080:8080 ascii-site`.
4. Validate `/`, `/text`, `/blog`, `/healthz` before pushing.
5. CI enforces tests + `dotnet list package --vulnerable`.

## 13. Open Questions & Future Enhancements
- Admin UI for blog/ASCII editing?
- Internationalization of ASCII slogans?
- Light/dark ASCII palettes toggled via user preference.
- WebSocket-driven live ASCII animations without breaking curl mode.

This plan should guide initial backlog creation and ensures any future implementation stays aligned with the repository’s Copilot instructions, including architecture, testing rigor, security posture, and Docker-first delivery.
