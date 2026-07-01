#!/bin/bash
# Runs the composition variety simulation against a live Ollama — synthetic play sessions ->
# real LLM composition -> variety assertions. No Unity, no server. See
# apps/api/ProjectAscension.Api.Tests/CompositionVarietySimulation.cs.
#
# Usage:
#   tools/discovery-variety-sim.sh [MODEL]
# Env:
#   OLLAMA_ENDPOINT (default http://100.96.108.30:11434)   OLLAMA_MODEL (default qwen3-coder:30b)
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ENDPOINT="${OLLAMA_ENDPOINT:-http://100.96.108.30:11434}"
MODEL="${1:-${OLLAMA_MODEL:-qwen3-coder:30b}}"

echo "Variety simulation — model=$MODEL endpoint=$ENDPOINT"
docker run --rm --network pa-net -v "$ROOT":/src -w /src \
  -e OLLAMA_ENDPOINT="$ENDPOINT" -e OLLAMA_MODEL="$MODEL" \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet test apps/api/ProjectAscension.Api.Tests \
    --filter FullyQualifiedName~CompositionVarietySimulation \
    --logger "console;verbosity=detailed" --nologo 2>&1 \
  | grep -vE "^\s*$" | grep -iE "play|delivery|distinct|Passed!|Failed!|SKIPPED|variety too low|composition deferred|model:"
