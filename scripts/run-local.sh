#!/usr/bin/env bash
# Runs the web app natively (dotnet run - NOT in Docker) against a local,
# disposable SQL Server container (started/seeded via db-up.sh). This is the
# local dev workflow - Docker is only used for (a) the disposable SQL Server
# dependency here, and (b) building the deployable webapp image (see
# DavidsonRFB.Attendance.Web/Dockerfile), never to run the app locally.
#
# Usage:
#   scripts/run-local.sh          # http only (launch profile "http")
#   scripts/run-local.sh --https  # http + https (launch profile "https",
#                                  # requires `dotnet dev-certs https --trust`
#                                  # to have been run once on this machine)
#
# Real AzureAd:TenantId/ClientId are read from .env if present (copy
# .env.example to .env and fill in real values - see that file for the
# app registration requirements, including which redirect URI to register
# for each launch profile).
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SA_PASSWORD="${SA_PASSWORD:-LocalDev_Passw0rd!}"
HOST_PORT="${SQL_HOST_PORT:-14330}"

if [ -f "${REPO_ROOT}/.env" ]; then
  set -a
  # shellcheck disable=SC1091
  source "${REPO_ROOT}/.env"
  set +a
fi

"${REPO_ROOT}/scripts/db-up.sh"

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ConnectionStrings__AttendanceContext="Server=localhost,${HOST_PORT};Database=Attendance;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True"

if [ -n "${AZURE_AD_TENANT_ID:-}" ]; then
  export AzureAd__TenantId="${AZURE_AD_TENANT_ID}"
fi
if [ -n "${AZURE_AD_CLIENT_ID:-}" ]; then
  export AzureAd__ClientId="${AZURE_AD_CLIENT_ID}"
fi

PROFILE="http"
if [ "${1:-}" = "--https" ]; then
  PROFILE="https"
fi

echo "Starting DavidsonRFB.Attendance.Web (launch profile: ${PROFILE})..."
dotnet run --project "${REPO_ROOT}/DavidsonRFB.Attendance.Web" --launch-profile "${PROFILE}"
