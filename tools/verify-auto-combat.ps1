param(
    [string]$DumpPath = "tools/generated/v15g7-auto-combat-dump.json",
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
        Invoke-GodotHidden @("--headless", "--path", $root, "--quit-after", "60")
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
$env:WUXIA_GODOT_STATE_DUMP_PATH = $absoluteDumpPath
try {
    Invoke-GodotHidden @("--path", $root, "--quit-after", "$QuitAfterFrames")
} finally {
    Remove-Item Env:\WUXIA_GODOT_PREVIEW_COMMANDS -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_STATE_DUMP_PATH -ErrorAction SilentlyContinue
}

Assert-True (Test-Path -LiteralPath $absoluteDumpPath) "Auto combat dump was not created: $absoluteDumpPath"

$state = Get-Content -LiteralPath $absoluteDumpPath -Raw | ConvertFrom-Json
$eventKinds = @($state.combat.events | ForEach-Object { $_.kind })

Assert-True ($state.saveSchemaVersion -eq "v15g.5") "Unexpected schema: $($state.saveSchemaVersion)"
Assert-True ($state.combat.turn -ge 2) "Auto combat did not advance at least two turns."
Assert-True ($state.combat.eventLog.Count -ge 2) "Auto combat event log did not record multiple events."
Assert-True ($state.combat.events.Count -ge 3) "Auto combat structured events did not record multiple events."
Assert-True ($eventKinds -contains "hit") "Auto combat did not record a hit event."
Assert-True ($eventKinds -contains "defeat") "Auto combat did not record a defeat event."
Assert-True ($eventKinds -contains "advance") "Auto combat did not record an advance event."
Assert-True ($state.stage.floor -gt 20) "Auto combat did not advance to the next floor."

Clear-PreviewSave

Write-Host "Auto combat assertions passed."
Write-Host "State dump: $absoluteDumpPath"
