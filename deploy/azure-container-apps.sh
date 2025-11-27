#!/usr/bin/env bash
set -euo pipefail

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI is required. Install from https://learn.microsoft.com/cli/azure/install-azure-cli" >&2
  exit 1
fi

RESOURCE_GROUP=${1:-ascii-site-rg}
LOCATION=${2:-eastus}
ACR_NAME=${3:-asciisiteacr}
CONTAINER_APP_NAME=${4:-ascii-site-app}
IMAGE_TAG=${5:-ascii-site:latest}
IMAGE_NAME=${6:-ascii-site}
PORT=${PORT:-8080}

FULL_IMAGE="$ACR_NAME.azurecr.io/${IMAGE_NAME}:$(date +%Y%m%d%H%M%S)"

echo "[1/6] Ensuring resource group $RESOURCE_GROUP exists"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" >/dev/null

echo "[2/6] Ensuring Azure Container Registry $ACR_NAME exists"
az acr show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1 || \
  az acr create --resource-group "$RESOURCE_GROUP" --name "$ACR_NAME" --sku Basic >/dev/null

echo "[3/6] Building container image using local Docker daemon"
docker build -t "$IMAGE_TAG" ..
docker tag "$IMAGE_TAG" "$FULL_IMAGE"

echo "[4/6] Pushing $FULL_IMAGE to ACR"
docker push "$FULL_IMAGE"

echo "[5/6] Ensuring Container Apps environment exists"
CONTAINERAPPS_ENV=${CONTAINERAPPS_ENV:-ascii-site-env}
az containerapp env create \
  --name "$CONTAINERAPPS_ENV" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" >/dev/null

echo "[6/6] Deploying Container App $CONTAINER_APP_NAME"
az containerapp create \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --environment "$CONTAINERAPPS_ENV" \
  --image "$FULL_IMAGE" \
  --target-port "$PORT" \
  --ingress external \
  --registry-server "$ACR_NAME.azurecr.io" \
  --min-replicas 1 --max-replicas 3 \
  --env-vars "ASPNETCORE_ENVIRONMENT=Production" "ApplicationInsights__ConnectionString=${APPLICATIONINSIGHTS_CONNECTION_STRING:-}" >/dev/null

echo "Deployment complete. Use 'az containerapp show --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn -o tsv' to retrieve the public URL."
