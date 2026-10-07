<#
 Builds Rhino Wood, runs the automated tests and creates dist\RhinoWood-<version>\ (plugin) and a .zip.
 Requires: .NET SDK 8 (dotnet). Rhino 8 for Windows is only needed to RUN the plugin, not to build it
 (RhinoCommon comes from NuGet).
#>
param([string]$Configuration = 'Release', [switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
  dotnet restore RhinoWood.sln
  if (-not $SkipTests) { dotnet test tests/RhinoWood.Tests/RhinoWood.Tests.csproj -c $Configuration; if ($LASTEXITCODE) { throw 'Tests failed' } }
  dotnet build src/RhinoWood.Plugin/RhinoWood.Plugin.csproj -c $Configuration
  if ($LASTEXITCODE) { throw 'Plugin build failed' }
  $ver = '1.1.0'
  $out = Join-Path $root "dist\RhinoWood-$ver"
  if (Test-Path $out) { Remove-Item $out -Recurse -Force }
  New-Item -ItemType Directory -Path $out | Out-Null
  Copy-Item "src\RhinoWood.Plugin\bin\$Configuration\net7.0-windows\*" $out -Recurse
  Copy-Item "manifest.yml" $out
  Compress-Archive -Path "$out\*" -DestinationPath "dist\RhinoWood-$ver.zip" -Force
  Write-Host "Built: $out" -ForegroundColor Green
} finally { Pop-Location }
