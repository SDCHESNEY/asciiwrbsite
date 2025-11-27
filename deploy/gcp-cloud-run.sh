#!/usr/bin/env bash
set -euo pipefail

if ! command -v gcloud >/dev/null 2>&1; then
  echo "gcloud CLI is required. Install from https://cloud.google.com/sdk/docs/install" >&2
  exit 1
fi

PROJECT_ID=${1:-ascii-site}
REGION=${2:-us-central1}
SERVICE_NAME=${3:-ascii-site}
IMAGE_NAME=${4:-gcr.io/$PROJECT_ID/ascii-site}
PORT=${PORT:-8080}

if [[ -z "${GOOGLE_APPLICATION_CREDENTIALS:-}" ]]; then
  echo "GOOGLE_APPLICATION_CREDENTIALS must be set to a service account key json." >&2
  exit 1
fi

export CLOUDSDK_CORE_PROJECT="$PROJECT_ID"

echo "[1/5] Enabling required services"
gcloud services enable run.googleapis.com artifactregistry.googleapis.com logging.googleapis.com >/dev/null

echo "[2/5] Building container image"
docker build -t "$IMAGE_NAME" ..

echo "[3/5] Pushing container image to Artifact Registry"
docker push "$IMAGE_NAME"

echo "[4/5] Deploying to Cloud Run"
gcloud run deploy "$SERVICE_NAME" \
  --image "$IMAGE_NAME" \
  --region "$REGION" \
  --port "$PORT" \
  --allow-unauthenticated \
  --set-env-vars "ASPNETCORE_ENVIRONMENT=Production,ApplicationInsights__ConnectionString=${APPLICATIONINSIGHTS_CONNECTION_STRING:-}" >/dev/null

echo "[5/5] Deployment complete"
gcloud run services describe "$SERVICE_NAME" --region "$REGION" --format='value(status.url)'
