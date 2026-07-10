$ErrorActionPreference = "Stop"

$script:GodotRoot = $env:WUXIA_GODOT_ROOT
if (!$script:GodotRoot) {
  $script:GodotRoot = [Environment]::GetEnvironmentVariable("WUXIA_GODOT_ROOT", "User")
}
if (!$script:GodotRoot) {
  $script:GodotRoot = [Environment]::GetEnvironmentVariable("WUXIA_GODOT_ROOT", "Machine")
}
if (!$script:GodotRoot) {
  $documents = [Environment]::GetFolderPath("MyDocuments")
  $found = Get-ChildItem -LiteralPath $documents -Filter "Godot_v4.7-stable_mono_win64_console.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($found) {
    $script:GodotRoot = $found.DirectoryName
  }
}

if (!$script:GodotRoot) {
  throw "Godot root not found. Set WUXIA_GODOT_ROOT to the folder containing Godot_v4.7-stable_mono_win64_console.exe."
}

$script:GodotExe = Join-Path $script:GodotRoot "Godot_v4.7-stable_mono_win64_console.exe"
$script:GodotEditorExe = Join-Path $script:GodotRoot "Godot_v4.7-stable_mono_win64.exe"

if (!(Test-Path -LiteralPath $script:GodotExe)) {
  throw "Godot console executable not found: $script:GodotExe"
}

if (!(Test-Path -LiteralPath (Join-Path $script:GodotRoot "GodotSharp"))) {
  throw "GodotSharp folder is missing next to the Godot executable: $script:GodotRoot"
}

$env:PATH = "$script:GodotRoot;$env:PATH"

$dotnetRoot = "C:\Program Files\dotnet"
if (Test-Path -LiteralPath (Join-Path $dotnetRoot "dotnet.exe")) {
  $env:DOTNET_ROOT = $dotnetRoot
  if ($env:PATH -notlike "*$dotnetRoot*") {
    $env:PATH = "$dotnetRoot;$env:PATH"
  }
}

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
$machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
foreach ($pathPart in @($userPath, $machinePath)) {
  if ($pathPart) {
    foreach ($entry in $pathPart.Split(";")) {
      if ($entry -and $env:PATH -notlike "*$entry*") {
        $env:PATH = "$entry;$env:PATH"
      }
    }
  }
}

function Invoke-WuxiaGodot {
  & $script:GodotExe @args
}

function Open-WuxiaGodotEditor {
  param(
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
  )
  Start-Process -FilePath $script:GodotEditorExe -ArgumentList @("--path", $ProjectPath)
}
