#!/usr/bin/env bash
#
# Headless compile-check for the Unity CLIENT shell (Assets MonoBehaviours) WITHOUT a
# Unity license. Unity's IDE-generated .csproj reference every assembly by HintPath and set
# NoStdLib/DisableImplicitFrameworkReferences, so plain `dotnet build` (Roslyn) can compile
# them against the editor's managed DLLs — no editor, no license, no entitlement.
#
# The gameplay LOGIC lives in packages/* (pure .NET, built in Docker already). This covers the
# remaining gap: the UnityEngine-referencing shell code the .NET builds and headless sims can't see.
#
# Usage:  tools/unity-compile-check.sh          # build all runtime shell asmdef projects
#         tools/unity-compile-check.sh Foo Bar   # build only these <name>.csproj
#
# Requires: Docker (mcr.microsoft.com/dotnet/sdk:9.0) + a local Unity editor matching
# apps/client_unity/ProjectSettings/ProjectVersion.txt. Regenerates a lean DLL mirror
# (~40M, only the referenced assemblies) under ~/.unity-ref/<version> on first run.
#
# NOTE: the .csproj are Unity-generated from the CURRENT Assets on the machine that last opened
# the project. After adding/removing .cs files, reopen the project (or let VSCode's Unity
# integration regenerate) so the Compile item list is current before trusting a green result.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CLIENT="$REPO/apps/client_unity"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$CLIENT/ProjectSettings/ProjectVersion.txt" | tr -d '[:space:]')"
BASE="/Applications/Unity/Hub/Editor/$VERSION"
MIRROR="$HOME/.unity-ref/$VERSION"

DEFAULT_PROJECTS=(ProjectAscension.Game ProjectAscension.Combat ProjectAscension.Monsters \
                  ProjectAscension.Equipment ProjectAscension.Core ProjectAscension.Net)
PROJECTS=("${@:-}")
[ -z "${PROJECTS[0]:-}" ] && PROJECTS=("${DEFAULT_PROJECTS[@]}")

# --- Build/refresh the lean DLL mirror from the csproj HintPaths + Analyzer refs. ---
if [ ! -d "$MIRROR" ] || [ -z "$(ls -A "$MIRROR" 2>/dev/null)" ]; then
  [ -d "$BASE" ] || { echo "ERROR: Unity $VERSION not found at $BASE"; exit 2; }
  echo "Building DLL mirror for $VERSION (first run)…"
  mkdir -p "$MIRROR"
  grep -rhoE "$BASE/[^<\"]+\.dll" "$CLIENT"/*.csproj | sort -u | while IFS= read -r f; do
    [ -f "$f" ] || continue
    dst="$MIRROR/${f#"$BASE"/}"; mkdir -p "$(dirname "$dst")"; cp "$f" "$dst"
  done
  echo "  mirror: $(du -sh "$MIRROR" | cut -f1) at $MIRROR"
fi

# --- Compile each project headless in Docker (repo mounted at its real path so the csproj's
#     absolute Compile paths resolve; mirror mounted where the HintPaths expect the editor).
#     Projects are passed as args to the in-container script ($@), keeping stdin free for the
#     heredoc. ---
docker run --rm -i \
  -v "$REPO":"$REPO" \
  -v "$MIRROR:$BASE:ro" \
  -w "$CLIENT" \
  mcr.microsoft.com/dotnet/sdk:9.0 bash -s "${PROJECTS[@]}" <<'DOCKER'
fail=0
for proj in "$@"; do
  [ -z "$proj" ] && continue
  if [ ! -f "$proj.csproj" ]; then printf "%-30s MISSING csproj\n" "$proj"; fail=1; continue; fi
  out=$(dotnet build "$proj.csproj" -v q -nologo 2>&1)
  if echo "$out" | grep -q "Build succeeded"; then
    printf "%-30s OK\n" "$proj"
  else
    printf "%-30s FAILED\n" "$proj"
    echo "$out" | grep -E "error " | grep -viE "warning" | sed 's/^/    /' | head -20
    fail=1
  fi
done
exit $fail
DOCKER