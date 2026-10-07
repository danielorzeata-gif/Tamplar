<# Copies this repository into C:\Users\<you>\Desktop\Rhino Wood (the working folder requested for the project). #>
param([string]$Target = (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Rhino Wood'))
$root = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Path $Target -Force | Out-Null
robocopy $root $Target /E /XD .git bin obj dist .vs /NFL /NDL /NJH /NJS | Out-Null
Write-Host "Project copied to $Target" -ForegroundColor Green
