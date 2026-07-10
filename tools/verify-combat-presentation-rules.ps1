param(
    [string]$Output = "screenshots/v15g12-combat-presentation-rules-preview.png",
    [string]$StateDumpPath = "tools/generated/v15g12-combat-presentation-state-dump.json",
    [string]$TimelineDumpPath = "tools/generated/v15g12-combat-presentation-timeline-dump.json",
    [int]$CaptureAfterFrames = 1260,
    [int]$QuitAfterFrames = 1320
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "godot-env.ps1")

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$absoluteOutputPath = Join-Path $root $Output
$absoluteStateDumpPath = Join-Path $root $StateDumpPath
$absoluteTimelineDumpPath = Join-Path $root $TimelineDumpPath
$rulesPath = Join-Path $root "data/combat-presentation-rules.json"

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

function Assert-Close {
    param(
        [double]$Actual,
        [double]$Expected,
        [string]$Message
    )

    if ([Math]::Abs($Actual - $Expected) -gt 0.001) {
        throw "$Message Actual=$Actual Expected=$Expected"
    }
}

function Get-TraceStep {
    param(
        [object]$Timeline,
        [string]$Phase
    )

    return @($Timeline.trace) | Where-Object { $_.phase -eq $Phase } | Select-Object -First 1
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

$rules = Get-Content -LiteralPath $rulesPath -Raw -Encoding UTF8 | ConvertFrom-Json
Assert-True ($rules.version -eq "v15g.12") "Combat presentation rules version is not v15g.12: $($rules.version)"
Assert-True ($null -ne $rules.rewardNoticePresentation.colors.sect_notice) "Missing sect_notice presentation color."
Assert-True ($null -ne $rules.rewardNoticePresentation.colors.adventure_notice) "Missing adventure_notice presentation color."

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

Assert-True (Test-Path -LiteralPath $absoluteOutputPath) "Combat presentation screenshot was not created: $absoluteOutputPath"
Assert-True (Test-Path -LiteralPath $absoluteStateDumpPath) "Combat presentation state dump was not created: $absoluteStateDumpPath"
Assert-True (Test-Path -LiteralPath $absoluteTimelineDumpPath) "Combat presentation timeline dump was not created: $absoluteTimelineDumpPath"

$state = Get-Content -LiteralPath $absoluteStateDumpPath -Raw -Encoding UTF8 | ConvertFrom-Json
$timeline = Get-Content -LiteralPath $absoluteTimelineDumpPath -Raw -Encoding UTF8 | ConvertFrom-Json
$floor = [int]$state.stage.floor
$expectedBand = @($rules.enemyVisualProfiles.floorBands) |
    Where-Object { $floor -ge [int]$_.minFloor } |
    Sort-Object { [int]$_.minFloor } -Descending |
    Select-Object -First 1
$weaponModulo = [Math]::Abs($floor) % 5
$expectedWeapon = @($rules.enemyVisualProfiles.weaponCycle) |
    Where-Object { [int]$_.floorModulo -eq $weaponModulo } |
    Select-Object -First 1

$defeatStep = Get-TraceStep $timeline "defeat_pause"
$rewardStep = Get-TraceStep $timeline "reward_settlement"
$spawnStep = Get-TraceStep $timeline "enemy_spawn"
$advanceEvent = @($state.combat.events) | Where-Object { $_.kind -eq "advance" } | Select-Object -First 1

Assert-True ($state.saveSchemaVersion -eq "v15g.5") "Unexpected schema: $($state.saveSchemaVersion)"
Assert-True ($floor -gt 20) "Combat presentation preview did not reach the next floor."
Assert-True ($null -ne $advanceEvent) "Structured advance event was not recorded."
Assert-True ($null -ne $expectedBand) "No configured enemy visual band matched floor $floor."
Assert-True ($null -ne $expectedWeapon) "No configured weapon cycle entry matched modulo $weaponModulo."
Assert-True ($timeline.enemyProfileId -eq $expectedBand.id) "Enemy profile id did not come from presentation rules: $($timeline.enemyProfileId) vs $($expectedBand.id)"
Assert-True ($timeline.enemyBodyTexturePath -eq $expectedBand.bodyTexturePath) "Enemy body texture did not come from presentation rules."
Assert-True ($timeline.enemyWeaponTexturePath -eq $expectedWeapon.weaponTexturePath) "Enemy weapon texture did not come from presentation rules."
Assert-True ($null -ne $defeatStep) "Timeline did not record defeat_pause."
Assert-True ($null -ne $rewardStep) "Timeline did not record reward_settlement."
Assert-True ($null -ne $spawnStep) "Timeline did not record enemy_spawn."
Assert-Close ([double]$defeatStep.holdSeconds) ([double]$rules.timelineDurations.defeatPause) "defeat_pause duration did not match rules."
Assert-Close ([double]$rewardStep.holdSeconds) ([double]$rules.timelineDurations.rewardSettlement) "reward_settlement duration did not match rules."
Assert-Close ([double]$spawnStep.holdSeconds) ([double]$rules.timelineDurations.enemySpawn) "enemy_spawn duration did not match rules."

Clear-PreviewSave

Write-Host "Combat presentation rules assertions passed."
Write-Host "Profile: $($timeline.enemyProfileId)"
Write-Host "Weapon: $($timeline.enemyWeaponTexturePath)"
Write-Host "Captured combat presentation preview: $absoluteOutputPath"
Write-Host "Timeline dump: $absoluteTimelineDumpPath"
