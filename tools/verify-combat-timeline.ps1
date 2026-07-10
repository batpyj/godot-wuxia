param(
    [string]$Output = "screenshots/v15g9-combat-timeline-preview.png",
    [string]$StateDumpPath = "tools/generated/v15g9-combat-timeline-state-dump.json",
    [string]$TimelineDumpPath = "tools/generated/v15g9-combat-timeline-dump.json",
    [int]$CaptureAfterFrames = 1260,
    [int]$QuitAfterFrames = 1320
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "godot-env.ps1")

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$absoluteOutputPath = Join-Path $root $Output
$absoluteStateDumpPath = Join-Path $root $StateDumpPath
$absoluteTimelineDumpPath = Join-Path $root $TimelineDumpPath

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

foreach ($path in @($absoluteOutputPath, $absoluteStateDumpPath, $absoluteTimelineDumpPath)) {
    $directory = Split-Path -Parent $path
    if (!(Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

Clear-PreviewSave

$env:WUXIA_GODOT_PREVIEW_COMMANDS = "auto"
$env:WUXIA_GODOT_CAPTURE_PATH = $Output
$env:WUXIA_GODOT_CAPTURE_AFTER_FRAMES = "$CaptureAfterFrames"
$env:WUXIA_GODOT_STATE_DUMP_PATH = $absoluteStateDumpPath
$env:WUXIA_GODOT_TIMELINE_DUMP_PATH = $absoluteTimelineDumpPath
try {
    Invoke-GodotHidden @("--path", $root, "--quit-after", "$QuitAfterFrames")
} finally {
    Remove-Item Env:\WUXIA_GODOT_PREVIEW_COMMANDS -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_CAPTURE_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_CAPTURE_AFTER_FRAMES -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_STATE_DUMP_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_TIMELINE_DUMP_PATH -ErrorAction SilentlyContinue
}

Assert-True (Test-Path -LiteralPath $absoluteOutputPath) "Combat timeline screenshot was not created: $absoluteOutputPath"
Assert-True (Test-Path -LiteralPath $absoluteStateDumpPath) "Combat timeline state dump was not created: $absoluteStateDumpPath"
Assert-True (Test-Path -LiteralPath $absoluteTimelineDumpPath) "Combat timeline dump was not created: $absoluteTimelineDumpPath"

$state = Get-Content -LiteralPath $absoluteStateDumpPath -Raw | ConvertFrom-Json
$timeline = Get-Content -LiteralPath $absoluteTimelineDumpPath -Raw | ConvertFrom-Json
$phaseNames = @($timeline.trace | ForEach-Object { $_.phase })
$advanceEvent = @($state.combat.events) | Where-Object { $_.kind -eq "advance" } | Select-Object -First 1

Assert-True ($state.saveSchemaVersion -eq "v15g.5") "Unexpected schema: $($state.saveSchemaVersion)"
Assert-True ($state.stage.floor -gt 20) "Combat timeline preview did not reach the next floor."
Assert-True ($null -ne $advanceEvent) "Structured advance event was not recorded."
Assert-True ($phaseNames -contains "defeat_pause") "Timeline did not record defeat_pause."
Assert-True ($phaseNames -contains "reward_settlement") "Timeline did not record reward_settlement."
Assert-True ($phaseNames -contains "enemy_spawn") "Timeline did not record enemy_spawn."
Assert-True ($phaseNames -contains "next_beat_ready") "Timeline did not record next_beat_ready."
Assert-True ([double]$timeline.autoCombatIntervalSeconds -gt 0) "Timeline dump did not include auto combat interval."

Clear-PreviewSave

Write-Host "Combat timeline assertions passed."
Write-Host "Captured combat timeline preview: $absoluteOutputPath"
Write-Host "Timeline dump: $absoluteTimelineDumpPath"
