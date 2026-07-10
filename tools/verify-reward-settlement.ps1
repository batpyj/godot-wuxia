param(
    [string]$Output = "screenshots/v15g10-reward-settlement-preview.png",
    [string]$StateDumpPath = "tools/generated/v15g10-reward-settlement-state-dump.json",
    [string]$TimelineDumpPath = "tools/generated/v15g10-reward-settlement-timeline-dump.json",
    [int]$CaptureAfterFrames = 820,
    [int]$QuitAfterFrames = 880
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

Assert-True (Test-Path -LiteralPath $absoluteOutputPath) "Reward settlement screenshot was not created: $absoluteOutputPath"
Assert-True (Test-Path -LiteralPath $absoluteStateDumpPath) "Reward settlement state dump was not created: $absoluteStateDumpPath"
Assert-True (Test-Path -LiteralPath $absoluteTimelineDumpPath) "Reward settlement timeline dump was not created: $absoluteTimelineDumpPath"

$state = Get-Content -LiteralPath $absoluteStateDumpPath -Raw | ConvertFrom-Json
$timeline = Get-Content -LiteralPath $absoluteTimelineDumpPath -Raw | ConvertFrom-Json
$eventKinds = @($state.combat.events | ForEach-Object { $_.kind })
$timelinePhases = @($timeline.trace | ForEach-Object { $_.phase })

Assert-True ($state.saveSchemaVersion -eq "v15g.5") "Unexpected schema: $($state.saveSchemaVersion)"
Assert-True ($state.combat.lastHitKind -eq "defeat") "Reward settlement should capture the defeat settlement beat."
Assert-True ($eventKinds -contains "reward") "Reward event was not recorded."
Assert-True ($eventKinds -contains "drop") "Drop event was not recorded."
Assert-True ($eventKinds -contains "sect_notice") "Sect notice event was not recorded."
Assert-True ($eventKinds -contains "adventure_notice") "Adventure notice event was not recorded."
Assert-True ($timelinePhases -contains "reward_settlement") "Timeline did not enter reward_settlement."
Assert-True ($state.hero.resources.silver -gt 3589) "Silver reward was not applied."
Assert-True ($state.hero.resources.essence -gt 62) "Essence drop was not applied."
Assert-True ($state.hero.cultivation -gt 4357) "Cultivation reward was not applied."
Assert-True ($state.hero.jianghuQualities.reputation -gt 47) "Sect reputation reward was not applied."

Clear-PreviewSave

Write-Host "Reward settlement assertions passed."
Write-Host "Captured reward settlement preview: $absoluteOutputPath"
Write-Host "State dump: $absoluteStateDumpPath"
