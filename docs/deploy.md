# Deployment Guide

This guide explains how to run ASCII Site in containers locally, then push the production build to Azure Container Apps or Google Cloud Run. All flows reuse the multi-stage `Dockerfile` at the repo root.

## Prerequisites
- Docker 24+
- .NET 9 SDK for local testing (`dotnet test AsciiSite.sln --configuration Release` before shipping)
- Azure CLI (`az`) for Azure deployments
- Google Cloud CLI (`gcloud`) for Cloud Run deployments
- Optional: `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable (kept outside source control) for server-side telemetry

## Local container run
```bash
# Build the image
DOCKER_BUILDKIT=1 docker build -t ascii-site:dev .

# Run locally on http://localhost:8080
 docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ApplicationInsights__ConnectionString="$APPLICATIONINSIGHTS_CONNECTION_STRING" \
  ascii-site:dev
```

The container exposes `/health`, `/text`, `/feed`, `/metrics`, and the Blazor UI on port 8080. Health and metrics endpoints are wired into readiness checks in `docker-compose.yml`.

## docker-compose quick start
```bash
docker compose up --build
```

The compose stack builds the image, exposes port 8080, and wires health checks so orchestration platforms can detect unhealthy instances.

## Azure Container Apps
Use `deploy/azure-container-apps.sh` to create or reuse infrastructure and push a new image:
```bash
chmod +x deploy/azure-container-apps.sh
APPLICATIONINSIGHTS_CONNECTION_STRING="<connection-string>" \
  deploy/azure-container-apps.sh ascii-site-rg eastus asciisiteacr ascii-site-app
```

The script performs the following:
1. Creates the resource group + Azure Container Registry (if needed).
2. Builds/pushes the Docker image to ACR.
3. Creates a Container Apps environment and deploys the new revision with autoscale (1–3 replicas) and port 8080 exposed.
4. Passes observability secrets via environment variables.

Retrieve the public FQDN with:
```bash
az containerapp show --name ascii-site-app --resource-group ascii-site-rg --query properties.configuration.ingress.fqdn -o tsv
```

## Google Cloud Run
Use `deploy/gcp-cloud-run.sh` with a service account authenticated via `GOOGLE_APPLICATION_CREDENTIALS`:
```bash
chmod +x deploy/gcp-cloud-run.sh
export GOOGLE_APPLICATION_CREDENTIALS=$HOME/.config/gcloud/application_default_credentials.json
APPLICATIONINSIGHTS_CONNECTION_STRING="" \
  deploy/gcp-cloud-run.sh ascii-site us-central1 ascii-site gcr.io/ascii-site/ascii-site
```

The script:
1. Enables required GCP services (Run, Artifact Registry, Logging).
2. Builds/pushes the container image.
3. Deploys a public Cloud Run service listening on port 8080 with required environment variables.
4. Outputs the public HTTPS URL of the new revision.

## Production hardening checklist
- Set `ASPNETCORE_ENVIRONMENT=Production` and supply `ApplicationInsights__ConnectionString` (or disable AI by omitting it).
- Configure TLS certificates / custom domains at the platform layer.
- Monitor `/health` for readiness and `/metrics` for Prometheus scraping or custom exporters.
- Store secrets (GitHub tokens, instrumentation keys) in Azure Key Vault or GCP Secret Manager; wire them into containers via environment variables or mounted secrets.
- Keep the Docker image up-to-date (`docker pull mcr.microsoft.com/dotnet/aspnet:9.0` regularly) and run `dotnet list package --vulnerable` before pushing new releases.
