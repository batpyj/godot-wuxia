param(
    [string]$Commands = "auto,martial,romance,weapon,combat_tick",
    [string]$Output = "screenshots/v15g7-combat-event-queue-preview.png"
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "godot-env.ps1")

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Invoke-GodotHidden {
    param([string[]]$Arguments)

    $process = Start-Process -FilePath $script:GodotExe -ArgumentList $Arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Godot command failed with exit code $($process.ExitCode): $($Arguments -join ' ')"
    }
}

$env:WUXIA_GODOT_PREVIEW_CLEAR_SAVE = "1"
try {
    Invoke-GodotHidden @("--headless", "--path", $root, "--quit-after", "60")
} finally {
    Remove-Item Env:\WUXIA_GODOT_PREVIEW_CLEAR_SAVE -ErrorAction SilentlyContinue
}

$env:WUXIA_GODOT_PREVIEW_COMMANDS = $Commands
$env:WUXIA_GODOT_CAPTURE_PATH = $Output
try {
    Invoke-GodotHidden @("--path", $root, "--quit-after", "60")
} finally {
    Remove-Item Env:\WUXIA_GODOT_PREVIEW_COMMANDS -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_CAPTURE_PATH -ErrorAction SilentlyContinue
}

$env:WUXIA_GODOT_PREVIEW_CLEAR_SAVE = "1"
try {
    Invoke-GodotHidden @("--headless", "--path", $root, "--quit-after", "60")
} finally {
    Remove-Item Env:\WUXIA_GODOT_PREVIEW_CLEAR_SAVE -ErrorAction SilentlyContinue
}

Write-Host "Executed commands: $Commands"
Write-Host "Captured command preview: $(Join-Path $root $Output)"
