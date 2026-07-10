param(
  [switch]$CheckAssetKeys
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\godot-env.ps1"

Write-Host "Godot executable:" $script:GodotExe
$godotVersion = & $script:GodotExe --version
Write-Host "Godot version:" $godotVersion

if ($godotVersion -notmatch "mono") {
  Write-Warning "This Godot build does not look like the .NET/Mono edition."
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnet) {
  Write-Host "dotnet:" $dotnet.Source
  dotnet --version
} else {
  Write-Warning "dotnet is missing from PATH. Godot can open, but C# project build will not work until .NET SDK is installed."
}

$ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
if ($ffmpeg) {
  Write-Host "ffmpeg:" $ffmpeg.Source
  ffmpeg -version | Select-Object -First 1
} else {
  Write-Warning "ffmpeg is missing from PATH. Proof-video export and video-to-frame tools will be limited."
}

if ($CheckAssetKeys) {
  $keys = "GOOGLE_API_KEY", "XAI_API_KEY", "TRIPO3D_API_KEY"
  foreach ($key in $keys) {
    $value = [Environment]::GetEnvironmentVariable($key, "Process")
    if (!$value) { $value = [Environment]::GetEnvironmentVariable($key, "User") }
    if (!$value) { $value = [Environment]::GetEnvironmentVariable($key, "Machine") }
    if ($value) {
      Write-Host "${key}: set"
    } else {
      Write-Warning "$key is missing. Paid asset generation through asset-gen will be unavailable."
    }
  }
} else {
  Write-Host "Asset API keys: skipped"
}
