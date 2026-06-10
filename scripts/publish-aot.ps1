param(
    [string]$Runtime = "win-x64",
    [ValidateSet("all", "console", "desktop")]
    [string]$Target = "all"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

$common = @(
    "-c", "Release",
    "-r", $Runtime,
    "--self-contained",
    "-v", "minimal"
)

function Publish-Project([string]$path) {
    Write-Host "Publishing $path ($Runtime)..." -ForegroundColor Cyan
    dotnet publish (Join-Path $root $path) @common
}

if ($Target -eq "all" -or $Target -eq "console") {
    Publish-Project "examples/DotKernel.Example/DotKernel.Example.csproj"
}

if ($Target -eq "all" -or $Target -eq "desktop") {
    Publish-Project "examples/DotKernel.AvaExample.Desktop/DotKernel.AvaExample.Desktop.csproj"
}

Write-Host ""
Write-Host "Done. Outputs:" -ForegroundColor Green
if ($Target -eq "all" -or $Target -eq "console") {
    Write-Host "  Console: examples/DotKernel.Example/bin/Release/net10.0/$Runtime/publish/"
}
if ($Target -eq "all" -or $Target -eq "desktop") {
    Write-Host "  Desktop: examples/DotKernel.AvaExample.Desktop/bin/Release/net10.0/$Runtime/publish/"
}
