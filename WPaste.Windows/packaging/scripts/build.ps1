Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $Root

& (Join-Path $PSScriptRoot "test.ps1")

$OutDir = Join-Path $Root "..\build\windows"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

dotnet publish "src\WPaste.Windows\WPaste.Windows.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:Platform=x64 `
    -o (Join-Path $OutDir "publish")

Write-Host "Published to $OutDir\publish"
