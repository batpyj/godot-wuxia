param(
    [switch]$Legacy,
    [string]$Output = "screenshots/v15g7-import-refresh.png"
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "godot-env.ps1")

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$generatedDir = Join-Path $root "tools\generated"
$saveTextPrefix = "WUXIA_SAVE_V15G0"
New-Item -ItemType Directory -Force -Path $generatedDir | Out-Null

function ConvertTo-CompactJson {
    param([object]$Value)
    return $Value | ConvertTo-Json -Depth 20 -Compress
}

function New-SaveText {
    param([object]$State)

    $json = ConvertTo-CompactJson $State
    $payload = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        $checksumBytes = $sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes("${saveTextPrefix}:$payload"))
    } finally {
        $sha256.Dispose()
    }

    $checksum = ([BitConverter]::ToString($checksumBytes) -replace "-", "").Substring(0, 16).ToLowerInvariant()
    return "${saveTextPrefix}::$payload::$checksum"
}

$currentState = [ordered]@{
    saveSchemaVersion = "v15g.5"
    sourceVersion = "v15g.7-import-refresh"
    stage = [ordered]@{
        floor = 32
        chapter = "Import Probe"
        martialRealm = "Probe Master"
        year = 11
        direction = "import refresh / field diff"
        combatPower = "32k"
    }
    hero = [ordered]@{
        name = "ImportHero"
        title = "Taiji Guest"
        sect = "Wudang"
        realm = "Probe Master"
        weapon = "sword"
        hp = 26000
        maxHp = 32000
        qi = 88
        comprehensivePower = 32000
        cultivation = 9876
        cultivationPerSecond = 26.5
        effects = @("taiji guard +18%", "sword intent +9%", "import probe")
        battleStats = [ordered]@{
            outerPower = 1888
            innerPower = 2666
            outerDefense = 1220
            innerDefense = 1680
            critRate = 35
            dodgeRate = 52
        }
        resources = [ordered]@{
            silver = 12345
            essence = 321
            food = 66
        }
        jianghuQualities = [ordered]@{
            reputation = 88
            comprehension = 91
            luck = 49
            charm = 57
            root = 72
            resolve = 80
        }
    }
    enemy = [ordered]@{
        name = "RefreshProbeEnemy"
        rank = "Floor 31"
        tag = "Probe"
        hp = 18888
        maxHp = 18888
        combatPower = "19k"
    }
    sectBonuses = @("inner power +18%", "movement +8%", "hp recovery +22%")
    martialArts = @(
        [ordered]@{
            name = "Taiji Core"
            type = "inner passive"
            level = "breakthrough 9/10"
            effect = "inner +180 / defense +64 / soften +12"
        },
        [ordered]@{
            name = "Soft Finger Sword"
            type = "sword art"
            level = "breakthrough 7/10"
            effect = "attack +96 / dodge +18 / sword intent +9%"
        }
    )
    romance = [ordered]@{
        name = "RenTingyu"
        favor = 42
        status = "assist ready"
        badge = "A"
    }
    weaponInfo = [ordered]@{
        title = "Weapon"
        name = "Darkiron Sword"
        type = "sword"
        score = 888
        socket = "independent socket / hand follow"
        effects = @("heavy sword trail", "attack score +888", "import probe weapon")
    }
    commands = [ordered]@{
        isPaused = $false
        autoExplore = $true
        activeMartialArtIndex = 1
        lastCommand = "imported command state"
    }
    combat = [ordered]@{
        turn = 0
        lastAction = "idle"
        lastTarget = "enemy"
        lastDamage = 0
        lastDamageRatio = 0
        lastHitKind = "none"
        enemyDefeated = $false
        message = "Combat ready"
        eventLog = @("import refresh probe")
        events = @(
            [ordered]@{
                turn = 0
                kind = "import"
                actor = "test"
                target = "godot"
                amount = 0
                message = "import refresh probe"
            }
        )
        migrationWarnings = @()
    }
}

$legacyState = [ordered]@{
    saveSchemaVersion = "v15g.0"
    sourceVersion = "v15g.0-legacy-import"
    stage = [ordered]@{
        floor = 24
        chapter = "Legacy Import"
        martialRealm = "Legacy Master"
        year = 9
        direction = "legacy migration / autofill"
        combatPower = "24k"
    }
    hero = [ordered]@{
        name = "LegacyHero"
        title = "Legacy Disciple"
        sect = "Huashan"
        realm = "Legacy Master"
        weapon = "sword"
        hp = 18000
        maxHp = 24000
        qi = 76
        comprehensivePower = 24000
        cultivation = 6789
        cultivationPerSecond = 21.0
    }
    enemy = [ordered]@{
        name = "LegacyEnemy"
        rank = "Floor 23"
        tag = "Legacy"
        hp = 9600
        maxHp = 9600
        combatPower = "9600"
    }
}

$currentSavePath = Join-Path $generatedDir "v15g7-current-save.txt"
$legacySavePath = Join-Path $generatedDir "v15g7-legacy-v15g0-save.txt"
$utf8NoBom = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText($currentSavePath, (New-SaveText $currentState), $utf8NoBom)
[IO.File]::WriteAllText($legacySavePath, (New-SaveText $legacyState), $utf8NoBom)

$selectedSavePath = if ($Legacy) { $legacySavePath } else { $currentSavePath }
$env:WUXIA_GODOT_PREVIEW_IMPORT_SAVE_PATH = $selectedSavePath
$env:WUXIA_GODOT_CAPTURE_PATH = $Output

try {
    $process = Start-Process -FilePath $script:GodotExe -ArgumentList @("--path", $root, "--quit-after", "60") -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Godot preview capture failed with exit code $($process.ExitCode)."
    }
} finally {
    Remove-Item Env:\WUXIA_GODOT_PREVIEW_IMPORT_SAVE_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:\WUXIA_GODOT_CAPTURE_PATH -ErrorAction SilentlyContinue
}

$env:WUXIA_GODOT_PREVIEW_CLEAR_SAVE = "1"
try {
    $clearProcess = Start-Process -FilePath $script:GodotExe -ArgumentList @("--headless", "--path", $root, "--quit-after", "60") -WindowStyle Hidden -Wait -PassThru
    if ($clearProcess.ExitCode -ne 0) {
        throw "Godot preview cleanup failed with exit code $($clearProcess.ExitCode)."
    }
} finally {
    Remove-Item Env:\WUXIA_GODOT_PREVIEW_CLEAR_SAVE -ErrorAction SilentlyContinue
}

Write-Host "Generated current save: $currentSavePath"
Write-Host "Generated legacy save: $legacySavePath"
Write-Host "Captured refresh preview: $(Join-Path $root $Output)"
