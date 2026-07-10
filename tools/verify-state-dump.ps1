param(
    [string]$Commands = "auto,martial,romance,weapon,combat_tick",
    [string]$DumpPath = "tools/generated/v15g7-state-dump.json"
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

$env:WUXIA_GODOT_PREVIEW_COMMANDS = $Commands
$env:WUXIA_GODOT_STATE_DUMP_PATH = $absoluteDumpPath
try {
    Invoke-GodotHidden @("--headless", "--path", $root, "--quit-after", "60")
} finally {
    Remove-Item Env:\WUXIA_GODOT_PREVIEW_COMMANDS -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_STATE_DUMP_PATH -ErrorAction SilentlyContinue
}

Assert-True (Test-Path -LiteralPath $absoluteDumpPath) "State dump was not created: $absoluteDumpPath"

$state = Get-Content -LiteralPath $absoluteDumpPath -Raw | ConvertFrom-Json

Assert-True ($state.saveSchemaVersion -eq "v15g.5") "Unexpected schema: $($state.saveSchemaVersion)"
Assert-True (($state.commands.autoExplore -eq $true) -or ($state.combat.enemyDefeated -eq $true)) "autoExplore was neither active nor stopped by victory."
Assert-True ($state.commands.activeMartialArtIndex -eq 1) "activeMartialArtIndex did not advance to 1."
Assert-True ($state.romance.favor -ge 16) "romance favor did not increase."
Assert-True ($state.weaponInfo.score -eq 888) "weapon score did not switch to 888."
Assert-True ($state.hero.weapon -eq "sword") "hero weapon key was not sword."
Assert-True ($state.combat.turn -ge 1) "combat turn did not advance."
Assert-True ($state.combat.lastDamage -gt 0) "combat damage was not recorded."
Assert-True ($state.combat.lastHitKind -in @("graze", "hit", "defeat")) "unexpected hit kind: $($state.combat.lastHitKind)"
Assert-True ($state.enemy.hp -lt $state.enemy.maxHp) "enemy hp did not decrease."
Assert-True ($state.combat.eventLog.Count -gt 0) "combat event log was not recorded."
Assert-True ($state.combat.events.Count -gt 0) "structured combat events were not recorded."

Clear-PreviewSave

Write-Host "State dump assertions passed."
Write-Host "Executed commands: $Commands"
Write-Host "State dump: $absoluteDumpPath"
