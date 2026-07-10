param(
    [string]$Output = "screenshots/v15g11-reward-rules-preview.png",
    [string]$StateDumpPath = "tools/generated/v15g11-reward-rules-state-dump.json",
    [string]$TimelineDumpPath = "tools/generated/v15g11-reward-rules-timeline-dump.json",
    [int]$CaptureAfterFrames = 820,
    [int]$QuitAfterFrames = 880
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "godot-env.ps1")

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$absoluteOutputPath = Join-Path $root $Output
$absoluteStateDumpPath = Join-Path $root $StateDumpPath
$absoluteTimelineDumpPath = Join-Path $root $TimelineDumpPath
$samplePath = Join-Path $root "data/combat-sample.json"
$rulesPath = Join-Path $root "data/reward-rules.json"

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

function Get-ObjectPropertyValue {
    param(
        [object]$Object,
        [string]$Name
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($property) {
        return $property.Value
    }

    return $null
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

$sample = Get-Content -LiteralPath $samplePath -Raw -Encoding UTF8 | ConvertFrom-Json
$rules = Get-Content -LiteralPath $rulesPath -Raw -Encoding UTF8 | ConvertFrom-Json
Assert-True ($rules.version -eq "v15g.11") "Reward rules version is not v15g.11: $($rules.version)"

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

Assert-True (Test-Path -LiteralPath $absoluteOutputPath) "Reward rules screenshot was not created: $absoluteOutputPath"
Assert-True (Test-Path -LiteralPath $absoluteStateDumpPath) "Reward rules state dump was not created: $absoluteStateDumpPath"
Assert-True (Test-Path -LiteralPath $absoluteTimelineDumpPath) "Reward rules timeline dump was not created: $absoluteTimelineDumpPath"

$state = Get-Content -LiteralPath $absoluteStateDumpPath -Raw -Encoding UTF8 | ConvertFrom-Json
$timeline = Get-Content -LiteralPath $absoluteTimelineDumpPath -Raw -Encoding UTF8 | ConvertFrom-Json
$floor = [int]$sample.stage.floor
$damage = [int]$state.combat.lastDamage
$formula = $rules.rewardFormula
$expectedSilverReward = [int]$formula.silverBase + $floor * [int]$formula.silverPerFloor + [Math]::Max(1, [int][Math]::Floor([int]$sample.enemy.maxHp / [Math]::Max(1, [int]$formula.silverEnemyHpDivisor)))
$expectedCultivationReward = [int]$formula.cultivationBase + $floor * [int]$formula.cultivationPerFloor + [Math]::Max(1, [int][Math]::Floor($damage / [Math]::Max(1, [int]$formula.cultivationDamageDivisor)))
$expectedEssenceReward = [int]$formula.essenceBase
if ([int]$formula.essenceBonusEveryFloors -gt 0 -and $floor % [int]$formula.essenceBonusEveryFloors -eq 0) {
    $expectedEssenceReward += [int]$formula.essenceBonus
}
$expectedFoodReward = 0
if ([int]$formula.foodBonusEveryFloors -gt 0 -and $floor % [int]$formula.foodBonusEveryFloors -eq 0) {
    $expectedFoodReward += [int]$formula.foodBonus
}
$expectedReputationReward = [Math]::Max([int]$formula.sectReputationMin, [int][Math]::Floor($floor / [Math]::Max(1, [int]$formula.sectReputationFloorDivisor)))
$dropName = Get-ObjectPropertyValue $rules.dropByTag $sample.enemy.tag
if (!$dropName) {
    $dropName = $rules.dropByTag.default
}

$rewardEvent = @($state.combat.events) | Where-Object { $_.kind -eq "reward" } | Select-Object -First 1
$dropEvent = @($state.combat.events) | Where-Object { $_.kind -eq "drop" } | Select-Object -First 1
$sectEvent = @($state.combat.events) | Where-Object { $_.kind -eq "sect_notice" } | Select-Object -First 1
$adventureEvent = @($state.combat.events) | Where-Object { $_.kind -eq "adventure_notice" } | Select-Object -First 1
$timelinePhases = @($timeline.trace | ForEach-Object { $_.phase })

Assert-True ($state.saveSchemaVersion -eq "v15g.5") "Unexpected schema: $($state.saveSchemaVersion)"
Assert-True ($state.combat.lastHitKind -eq "defeat") "Reward rules verification should capture defeat settlement."
Assert-True ($timelinePhases -contains "reward_settlement") "Timeline did not enter reward_settlement."
Assert-True ($rewardEvent.amount -eq $expectedSilverReward) "Reward silver event amount mismatch: $($rewardEvent.amount) vs $expectedSilverReward"
Assert-True ($dropEvent.amount -eq $expectedEssenceReward) "Drop event amount mismatch: $($dropEvent.amount) vs $expectedEssenceReward"
Assert-True ($sectEvent.amount -eq $expectedReputationReward) "Sect reputation event amount mismatch: $($sectEvent.amount) vs $expectedReputationReward"
Assert-True ($null -ne $adventureEvent) "Adventure notice event was not recorded."
Assert-True ($dropEvent.message -like "*$dropName*") "Drop event did not use configured drop name: $dropName"
Assert-True ([int]$state.hero.resources.silver -eq ([int]$sample.hero.resources.silver + $expectedSilverReward)) "Silver total did not match configured formula."
Assert-True ([int]$state.hero.resources.essence -eq ([int]$sample.hero.resources.essence + $expectedEssenceReward)) "Essence total did not match configured formula."
Assert-True ([int]$state.hero.resources.food -eq ([int]$sample.hero.resources.food + $expectedFoodReward)) "Food total did not match configured formula."
Assert-True ([int]$state.hero.cultivation -eq ([int]$sample.hero.cultivation + $expectedCultivationReward)) "Cultivation total did not match configured formula."
Assert-True ([int]$state.hero.jianghuQualities.reputation -eq ([int]$sample.hero.jianghuQualities.reputation + $expectedReputationReward)) "Reputation total did not match configured formula."

Clear-PreviewSave

Write-Host "Reward rules config assertions passed."
Write-Host "Configured drop: $dropName"
Write-Host "Captured reward rules preview: $absoluteOutputPath"
Write-Host "State dump: $absoluteStateDumpPath"
