<#
 Copies the plugin to a permanent folder (%APPDATA%\RhinoWood) that Rhino's Package Manager does NOT clean up.
 After that, load it once in Rhino (see the printed instructions); Rhino remembers it.
#>
param([string]$From)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $From) { $From = Join-Path $root 'dist\RhinoWood-1.0.0' }
if (-not (Test-Path (Join-Path $From 'RhinoWood.rhp'))) { throw "RhinoWood.rhp not found in $From - run scripts\build.ps1 first." }
$dest = Join-Path $env:APPDATA 'RhinoWood'
New-Item -ItemType Directory -Path $dest -Force | Out-Null
Copy-Item "$From\*" $dest -Recurse -Force
$rhp = Join-Path $dest 'RhinoWood.rhp'
Write-Host "Plugin copied to: $rhp" -ForegroundColor Green
Write-Host ""
Write-Host "In Rhino 8 (first time only):"
Write-Host "  1. Type the command  PlugInManager  (opens Options > Plug-ins)"
Write-Host "  2. Click 'Install...' and select:  $rhp"
Write-Host "  3. Type  WoodNewTable  (type it, do not paste)"
