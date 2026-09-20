#!/usr/bin/env bash
# Stops and removes the local dev SQL Server container started by db-up.sh.
# Pass --wipe to also delete its data volume (full reset - next db-up.sh will
# re-seed from scratch).
set -euo pipefail

CONTAINER_NAME="davidsonrfb-attendance-sql"
VOLUME_NAME="davidsonrfb-attendance-sql-data"

if docker rm -f "${CONTAINER_NAME}" >/dev/null 2>&1; then
  echo "Removed container ${CONTAINER_NAME}."
else
  echo "Container ${CONTAINER_NAME} was not running."
fi

if [ "${1:-}" = "--wipe" ]; then
  docker volume rm "${VOLUME_NAME}" >/dev/null 2>&1 && echo "Removed volume ${VOLUME_NAME}." || true
fi
