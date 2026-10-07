#!/usr/bin/env bash
# Runs the whole automated test-suite (works on Linux/macOS/Windows with the .NET 8 SDK).
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet test tests/RhinoWood.Tests/RhinoWood.Tests.csproj "$@"
