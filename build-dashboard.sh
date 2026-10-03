#!/usr/bin/env bash
set -euo pipefail

if [ -z "${1:-}" ]; then
    echo "Usage: build-dashboard.sh <tag>"
    echo "Example: build-dashboard.sh v0.12.0"
    exit 1
fi

TAG="$1"
IMAGE=jchristn77/assistanthub-dashboard

cd "$(dirname "${BASH_SOURCE[0]}")"

echo "Building and pushing ${IMAGE}:latest and ${IMAGE}:${TAG}..."
docker buildx build \
    --builder cloud-jchristn77-jchristn77 \
    --platform linux/amd64,linux/arm64/v8 \
    -t "${IMAGE}:latest" \
    -t "${IMAGE}:${TAG}" \
    -f dashboard/Dockerfile \
    --push \
    .

echo "Pulling ${IMAGE} into the local registry..."
docker pull "${IMAGE}:${TAG}"
docker pull "${IMAGE}:latest"

echo "Done."
