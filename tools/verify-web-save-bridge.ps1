param(
    [string]$InputPath = "data/combat-sample.json",
    [string]$OutputPath = "tools/generated/godot-save-from-combat-sample.txt",
    [string]$DumpPath = "tools/generated/v15g7-bridge-import-dump.json"
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "godot-env.ps1")

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$outputFullPath = Join-Path $root $OutputPath
$dumpFullPath = Join-Path $root $DumpPath

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

Clear-PreviewSave

powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "convert-web-save.ps1") -InputPath $InputPath -OutputPath $OutputPath

if (Test-Path -LiteralPath $dumpFullPath) {
    Remove-Item -LiteralPath $dumpFullPath -Force
}

$env:WUXIA_GODOT_PREVIEW_IMPORT_SAVE_PATH = $outputFullPath
$env:WUXIA_GODOT_STATE_DUMP_PATH = $dumpFullPath
try {
    Invoke-GodotHidden @("--headless", "--path", $root, "--quit-after", "60")
} finally {
    Remove-Item Env:\WUXIA_GODOT_PREVIEW_IMPORT_SAVE_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_STATE_DUMP_PATH -ErrorAction SilentlyContinue
}

Assert-True (Test-Path -LiteralPath $dumpFullPath) "Bridge import dump was not created: $dumpFullPath"

$state = Get-Content -LiteralPath $dumpFullPath -Raw | ConvertFrom-Json

Assert-True ($state.saveSchemaVersion -eq "v15g.5") "Unexpected schema: $($state.saveSchemaVersion)"
Assert-True ($state.sourceVersion -eq "web-bridge-v15g7") "Unexpected sourceVersion: $($state.sourceVersion)"
Assert-True ($state.stage.floor -eq 20) "Expected sample floor 20."
Assert-True ($state.martialArts.Count -ge 2) "Expected at least two imported martial arts."
Assert-True ($state.weaponInfo.score -eq 197) "Expected sample weapon score 197."
Assert-True ($state.combat.turn -eq 0) "Expected bridge import combat turn 0."
Assert-True ($state.combat.events.Count -gt 0) "Expected bridge import combat events."
Assert-True ($state.combat.migrationWarnings.Count -gt 0) "Expected migration warnings from bridge import."

Clear-PreviewSave

Write-Host "Web save bridge assertions passed."
Write-Host "Output save text: $outputFullPath"
Write-Host "Import dump: $dumpFullPath"
