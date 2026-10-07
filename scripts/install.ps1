<#
 Installs the plugin for the current user into Rhino 8's package folder (no admin needed) and prints how to load it.
 Run scripts\build.ps1 first (or pass -From to point at a built folder).
#>
param([string]$From)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $From) { $From = Join-Path $root 'dist\RhinoWood-1.0.0' }
if (-not (Test-Path (Join-Path $From 'RhinoWood.rhp'))) { throw "RhinoWood.rhp not found in $From - run scripts\build.ps1 first." }
$dest = Join-Path $env:APPDATA 'McNeel\Rhinoceros\packages\8.0\RhinoWood\1.0.0'
New-Item -ItemType Directory -Path $dest -Force | Out-Null
Copy-Item "$From\*" $dest -Recurse -Force
Write-Host "Installed to $dest" -ForegroundColor Green
Write-Host "Restart Rhino 8. If the plugin is not loaded automatically: Tools > Options > Plug-ins > Install... and select RhinoWood.rhp, or drag RhinoWood.rhp onto the Rhino window."
Write-Host "Then run:  WoodNewTable   (and WoodPanel for the dockable panel)"
