# ASCII Site Roadmap

This roadmap converts the architecture in `docs/idea.md` into a phased implementation plan with explicit deliverables, testing scope, and acceptance criteria aligned to `.github/copilot-instructions.md`.

## Delivery Status (Updated Nov 24, 2025)
| Phase | Status | Notes |
| --- | --- | --- |
| Phase 0 – Foundations & Tooling | ✅ Completed | Analyzers, shared props, HTTPS/HSTS/CSP, `/health`, and CI-parity test runs are in place. |
| Phase 1 – ASCII Core & Configuration | ✅ Completed | ASCII options/provider services, Hero & About components, curl/plaintext responses, and coverage for all acceptance criteria shipped. |
| Phase 2 – Blog Platform | ✅ Completed | Markdown blog provider, Blazor pages, `/feed`, and curl summaries shipped with tests. |
| Phase 3 – GitHub Showcase | ⏳ Not started | Pending kickoff. |
| Phase 4 – Polish, Observability, Deployment | ⏳ Not started | Pending kickoff. |
| Phase 5 – Future Enhancements | ⏳ Backlog | Optional stretch goals. |

## Phase 0 – Foundations & Tooling
**Goal:** Ensure the repo enforces coding standards, testing, and security pre-checks before feature work begins.

> **Status:** ✅ Completed in November 2025. Tooling, security baseline, and health endpoint are live, with integration tests verifying `/health` and security headers.

| Workstream | Tasks | Definition of Done |
| --- | --- | --- |
| Project Scaffolding | - Create `AsciiSite.Client`, `AsciiSite.Server` (optional), `AsciiSite.Shared` projects.<br>- Configure nullable reference types, global usings, analyzers, `Directory.Build.props` per instructions.<br>- Implement Program.cs extension methods for service registration and pipeline configuration. | - Solution builds cleanly with `dotnet build`.<br>- `dotnet format` passes with no changes.<br>- CI template or GitHub Actions pipeline runs `dotnet restore/build/test` and `dotnet list package --vulnerable`. |
| Testing Harness | - Add xUnit + bUnit + FluentAssertions packages.<br>- Configure shared `TestBase` helpers and deterministic fixture data builders.<br>- Add sample unit & component tests (e.g., Ascii options validator). | - `dotnet test --configuration Release` green locally and in CI.<br>- Test naming follows `MethodName_Condition_ExpectedResult`. |
| Security Baseline | - Enable HTTPS redirection, HSTS, CSP headers, and ProblemDetails middleware.<br>- Add `appsettings.Development.json` for local secrets, document user-secrets/Key Vault workflow. | - Security headers confirmed via integration test.<br>- Secrets not committed to repo. |

**Acceptance Criteria:**
- Build + test pipelines are reproducible locally and in CI.
- Basic health endpoint (`/healthz`) returns 200 and is covered by integration test.
- Documentation updated (`README`, `docs/idea.md`) to describe solution layout and workflows.

## Phase 1 – ASCII Core & Configuration
**Goal:** Deliver the ASCII hero, configuration binding, curl/plain text mode, and About page skeleton.

> **Status:** ✅ Completed in November 2025. The Hero/About experiences, plaintext mode, and markdown-driven content are shipping with unit, component, and integration coverage.

| Workstream | Tasks | Definition of Done |
| --- | --- | --- |
| ASCII Options & Services | - Define `AsciiArtOptions` with validation (length limits, fallback art).<br>- Bind options via `IOptionsSnapshot` and expose `AsciiArtProvider` service.<br>- Add admin CLI seed script or docs for editing `appsettings`. | - Unit tests cover validation rules and fallback behavior.<br>- `AsciiArtProvider` returns ASCII banner in both JSON array and escaped string formats. |
| Hero + About Components | - Build `Features/Ascii/Hero.razor` showing banner, tagline, nav CTA.<br>- Build `Features/About/AboutPage.razor` loading markdown from `content/about.md` via service. | - bUnit tests validate rendering, fallback art, and markdown conversion.<br>- Accessibility scan (e.g., axe) shows no blocking violations. |
| cURL / Plaintext Mode | - Add middleware or minimal API endpoint responding to `Accept: text/plain` or `/text` route.<br>- Compose plain-text output (banner, nav links, about summary) using Razor Class Library or string builder. | - Integration tests verify `/text` response, newline formatting, and 80-char width compliance.<br>- Browser mode unaffected (ShouldRender logic ensured). |

**Acceptance Criteria:**
- Landing page displays configurable ASCII art in browser and curl modes.
- About page pulls markdown content; editing `content/about.md` updates UI without rebuild (development hot reload).
- `curl http://localhost:8080/text` returns textual layout matching spec.

## Phase 2 – Blog Platform
**Goal:** Provide markdown-driven blog with routing, summaries, and optional RSS.

> **Status:** ✅ Completed in November 2025. Markdown posts with YAML frontmatter hydrate Blazor, curl mode, and `/feed` with cached parsing plus dev-time file watching.

| Workstream | Tasks | Definition of Done |
| --- | --- | --- |
| Content Pipeline | - Implement `FileSystemBlogPostProvider` reading `content/blog/{slug}.md` with YAML frontmatter (Markdig + YamlDotNet).<br>- Add caching + dev-time file watcher.<br>- Validate metadata (title, date, slug uniqueness) with structured logging. | - Unit tests cover parsing, validation failures, and duplicate slugs.<br>- Provider registered as singleton so caches are shared and invalidated on file change in Development. |
| UI Components | - `BlogIndex.razor` with pagination, tag filtering, and ASCII-styled list.<br>- `BlogPostPage.razor` for full article view with share links.<br>- Add RSS feed via Minimal API `/feed`. | - bUnit tests ensure binding of summaries/tags/empty states.<br>- Integration tests exercise `/blog`, `/blog/{slug}`, and `/feed`. |
| Curl Summaries | - Extend `/text` response to include latest N blog summaries (title, date, snippet). | - Plaintext tests verify inclusion of blog section with descending order and 80-char wrapping. |

**Acceptance Criteria:**
- Blog posts rendered in UI and via curl mode from the same markdown files.
- RSS feed validates via XML parsing tests that inspect `<rss>` and `<item>` nodes.
- Performance target: blog index loads under 200ms locally with caching enabled (singleton provider + pre-rendered HTML).

## Phase 3 – GitHub Showcase & External Integrations
**Goal:** Surface GitHub repositories with ASCII cards and optional live data from GitHub API.

| Workstream | Tasks | Definition of Done |
| --- | --- | --- |
| Repo Data Model | - Extend configuration to include default repo list (name, description, link).<br>- Create `GitHubRepoService` that can fetch live stats via typed `HttpClient` and fallback to config when API unavailable. | - Unit tests cover caching, fallback, and error logging.<br>- Secrets for GitHub token stored in Key Vault/Secret Manager (documented). |
| UI Components | - `GitHubShowcase.razor` rendering ASCII-bordered cards with CTA.
- Provide filters (language, topic) and display star count / last updated. | - bUnit tests verify card rendering, filtering, empty state messaging.<br>- Integration test mocks GitHub API (wiremock/test server) to validate caching + ProblemDetails. |
| Curl Integration | - Add repo list to `/text` output (name + URL). | - Plaintext coverage ensures repo section present even when API unreachable (fallback). |

**Acceptance Criteria:**
- GitHub section displays accurate data (manual QA vs API).
- Rate-limit behavior safe: cached values served, errors logged without user-facing stack traces.
- Security checks confirm no secrets in config; headers still enforced.

## Phase 4 – Polish, Observability, and Deployment Automation
**Goal:** Harden for production with logging, metrics, Docker delivery, and deployment scripts for Azure/GCP.

| Workstream | Tasks | Definition of Done |
| --- | --- | --- |
| Observability | - Configure structured logging (Serilog or ILogger providers) with correlation IDs.<br>- Add Application Insights (Azure) or Cloud Logging exporter (GCP).<br>- Implement `/metrics` or OTLP exporter (optional). | - Integration tests ensure correlation ID emitted; logging verified locally.<br>- `dotnet monitor` or OTLP documented for prod debugging. |
| Performance & Security | - Profile with Visual Studio diagnostics; add caching/compression middleware as needed.<br>- Final security audit: CSP tuning, SSRF protections, markdown sanitization review. | - Load test (k6 or similar) meets latency SLA.<br>- Security checklist signed off (OWASP Top 10 mitigations documented). |
| Docker & Deploy | - Author multi-stage Dockerfile + sample `docker-compose.yml`.
- Provide Azure (Bicep/ARM or CLI) and GCP (gcloud or Terraform) deploy scripts.
- Add GitHub Actions workflow to build/push image to ACR or GCR and deploy (manual approval). | - `docker build` + `docker run` used in CI to validate image.
- Sample deployment to test environment (Azure Container Apps or Cloud Run) documented with screenshots/CLI output.
- Health checks wired into platform (App Service/Cloud Run) hitting `/healthz`. |

**Acceptance Criteria:**
- Production deployment instructions tested end-to-end on at least one cloud provider.
- Monitoring confirms request logs and health checks visible.
- All documentation (`docs/idea.md`, `docs/roadmap.md`, README) reflects final architecture and operations guide.

## Phase 5 – Future Enhancements (Optional)
- Admin UI for managing ASCII art and blog posts.
- Localization/i18n for ASCII content.
- Animated ASCII effects via SignalR (with curl-safe fallback).
- Headless CMS integration (e.g., Contentful) for blog/posts.
- Additional themes (light/dark) with persisted user preference across browser + curl output.

## Verification Checklist (Per Release)
1. `dotnet format`, `dotnet build`, `dotnet test --configuration Release`, `dotnet list package --vulnerable` all pass.
2. Integration suite (`WebApplicationFactory`) covers `/`, `/text`, `/blog`, `/repo`, `/healthz`, `/feed`.
3. Security review confirms headers, HTTPS, auth policies, secret management.
4. Docker image built and scanned (Trivy or Microsoft Defender). 
5. Documentation updated; CHANGELOG entry created for each phase.

This roadmap keeps the backlog aligned with the architectural intent, ensuring each increment is testable, secure, and production-ready.