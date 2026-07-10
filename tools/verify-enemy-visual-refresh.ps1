param(
    [string]$Output = "screenshots/v15g8-enemy-visual-refresh-preview.png",
    [string]$DumpPath = "tools/generated/v15g8-enemy-visual-refresh-dump.json",
    [int]$CaptureAfterFrames = 1260,
    [int]$QuitAfterFrames = 1320
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "godot-env.ps1")

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$absoluteDumpPath = Join-Path $root $DumpPath
$absoluteOutputPath = Join-Path $root $Output
$dumpDirectory = Split-Path -Parent $absoluteDumpPath
$outputDirectory = Split-Path -Parent $absoluteOutputPath

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

foreach ($directory in @($dumpDirectory, $outputDirectory)) {
    if (!(Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }
}

foreach ($path in @($absoluteDumpPath, $absoluteOutputPath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
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

Assert-True (Test-Path -LiteralPath $absoluteDumpPath) "Enemy visual refresh dump was not created: $absoluteDumpPath"
Assert-True (Test-Path -LiteralPath $absoluteOutputPath) "Enemy visual refresh screenshot was not created: $absoluteOutputPath"

$state = Get-Content -LiteralPath $absoluteDumpPath -Raw | ConvertFrom-Json
$events = @($state.combat.events)
$advanceEvent = $events | Where-Object { $_.kind -eq "advance" } | Select-Object -First 1

Assert-True ($state.saveSchemaVersion -eq "v15g.5") "Unexpected schema: $($state.saveSchemaVersion)"
Assert-True ($state.stage.floor -gt 20) "Enemy visual refresh preview did not reach the next floor."
Assert-True ($null -ne $advanceEvent) "Structured advance event was not recorded."
Assert-True ($state.enemy.maxHp -gt 5312) "Enemy did not refresh to a generated next-floor target."

Clear-PreviewSave

Write-Host "Captured enemy visual refresh preview: $absoluteOutputPath"
Write-Host "State dump: $absoluteDumpPath"
