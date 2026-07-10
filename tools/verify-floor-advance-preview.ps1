param(
    [string]$Output = "screenshots/v15g7-floor-advance-preview.png",
    [string]$DumpPath = "tools/generated/v15g7-floor-advance-preview-dump.json",
    [int]$CaptureAfterFrames = 1260,
    [int]$QuitAfterFrames = 1320
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "godot-env.ps1")

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$absoluteDumpPath = Join-Path $root $DumpPath
$dumpDirectory = Split-Path -Parent $absoluteDumpPath

function Invoke-GodotHidden {
    param([string[]]$Arguments)

    $process = Start-Process -FilePath $script:GodotExe -ArgumentList $Arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Godot command failed with exit code $($process.ExitCode): $($Arguments -join ' ')"
    }
}

function Clear-PreviewSave {
    $env:WUXIA_GODOT_PREVIEW_CLEAR_SAVE = "1"
    try {
        Invoke-GodotHidden @("--path", $root, "--quit-after", "60")
    } finally {
        Remove-Item Env:\WUXIA_GODOT_PREVIEW_CLEAR_SAVE -ErrorAction SilentlyContinue
    }
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (!$Condition) {
        throw $Message
    }
}

if (!(Test-Path -LiteralPath $dumpDirectory)) {
    New-Item -ItemType Directory -Force -Path $dumpDirectory | Out-Null
}

if (Test-Path -LiteralPath $absoluteDumpPath) {
    Remove-Item -LiteralPath $absoluteDumpPath -Force
}

Clear-PreviewSave

$env:WUXIA_GODOT_PREVIEW_COMMANDS = "auto"
$env:WUXIA_GODOT_CAPTURE_PATH = $Output
$env:WUXIA_GODOT_CAPTURE_AFTER_FRAMES = "$CaptureAfterFrames"
$env:WUXIA_GODOT_STATE_DUMP_PATH = $absoluteDumpPath
try {
    Invoke-GodotHidden @("--path", $root, "--quit-after", "$QuitAfterFrames")
} finally {
    Remove-Item Env:\WUXIA_GODOT_PREVIEW_COMMANDS -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_CAPTURE_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_CAPTURE_AFTER_FRAMES -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_STATE_DUMP_PATH -ErrorAction SilentlyContinue
}

Assert-True (Test-Path -LiteralPath $absoluteDumpPath) "Floor advance dump was not created: $absoluteDumpPath"
$state = Get-Content -LiteralPath $absoluteDumpPath -Raw | ConvertFrom-Json
Assert-True ($state.saveSchemaVersion -eq "v15g.5") "Unexpected schema: $($state.saveSchemaVersion)"
Assert-True ($state.stage.floor -gt 20) "Floor advance preview did not reach the next floor."
Assert-True ($state.combat.events.Count -gt 0) "Floor advance preview did not record structured events."

Clear-PreviewSave

Write-Host "Captured floor advance preview: $(Join-Path $root $Output)"
Write-Host "State dump: $absoluteDumpPath"
