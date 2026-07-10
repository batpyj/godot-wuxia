param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,
    [string]$OutputPath = "tools/generated/godot-save-from-web.txt"
)

$ErrorActionPreference = "Stop"

$saveTextPrefix = "WUXIA_SAVE_V15G0"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Resolve-ProjectPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $root $Path
}

function Get-PathValue {
    param(
        [object]$Object,
        [string]$Path
    )

    $cursor = $Object
    foreach ($part in $Path.Split(".")) {
        if ($null -eq $cursor) {
            return $null
        }

        if ($cursor -is [System.Collections.IDictionary]) {
            if (!$cursor.Contains($part)) {
                return $null
            }

            $cursor = $cursor[$part]
            continue
        }

        $property = $cursor.PSObject.Properties[$part]
        if ($null -eq $property) {
            return $null
        }

        $cursor = $property.Value
    }

    return $cursor
}

function Get-FirstValue {
    param(
        [object]$Object,
        [string[]]$Paths,
        [object]$Fallback
    )

    foreach ($path in $Paths) {
        $value = Get-PathValue $Object $path
        if ($null -eq $value) {
            continue
        }

        if ($value -is [string] -and [string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        return $value
    }

    return $Fallback
}

function Get-StringValue {
    param(
        [object]$Object,
        [string[]]$Paths,
        [string]$Fallback
    )

    $value = Get-FirstValue $Object $Paths $Fallback
    if ($null -eq $value) {
        return $Fallback
    }

    if ($value -is [string]) {
        return $value
    }

    if ($value -is [ValueType]) {
        return [string]$value
    }

    return $Fallback
}

function Get-IntValue {
    param(
        [object]$Object,
        [string[]]$Paths,
        [int]$Fallback
    )

    $value = Get-FirstValue $Object $Paths $Fallback
    if ($null -eq $value) {
        return $Fallback
    }

    if ($value -is [int]) {
        return $value
    }

    if ($value -is [long] -or $value -is [double] -or $value -is [decimal]) {
        return [int]$value
    }

    $text = ([string]$value) -replace "[^\d-]", ""
    $parsed = 0
    if ([int]::TryParse($text, [ref]$parsed)) {
        return $parsed
    }

    return $Fallback
}

function Get-DoubleValue {
    param(
        [object]$Object,
        [string[]]$Paths,
        [double]$Fallback
    )

    $value = Get-FirstValue $Object $Paths $Fallback
    if ($null -eq $value) {
        return $Fallback
    }

    if ($value -is [double] -or $value -is [decimal] -or $value -is [int] -or $value -is [long]) {
        return [double]$value
    }

    $parsed = 0.0
    if ([double]::TryParse([string]$value, [ref]$parsed)) {
        return $parsed
    }

    return $Fallback
}

function Get-StringArrayValue {
    param(
        [object]$Object,
        [string[]]$Paths,
        [string[]]$Fallback
    )

    $value = Get-FirstValue $Object $Paths $null
    if ($null -eq $value) {
        return $Fallback
    }

    if ($value -is [string]) {
        return @($value)
    }

    if ($value -is [System.Collections.IEnumerable]) {
        $items = @()
        foreach ($item in $value) {
            if ($null -ne $item -and ![string]::IsNullOrWhiteSpace([string]$item)) {
                $items += [string]$item
            }
        }

        if ($items.Count -gt 0) {
            return $items
        }
    }

    return $Fallback
}

function New-MartialArts {
    param([object]$WebSave)

    $source = Get-FirstValue $WebSave @("martialArts", "rightPanel.martialArts", "skills", "manuals", "arts", "kungfu") $null
    $arts = @()

    if ($null -ne $source -and !($source -is [string]) -and $source -is [System.Collections.IEnumerable]) {
        foreach ($item in $source) {
            if ($arts.Count -ge 4) {
                break
            }

            $name = if ($item -is [string]) { $item } else { Get-StringValue $item @("name", "title", "id") "Imported Art" }
            $type = if ($item -is [string]) { "web" } else { Get-StringValue $item @("type", "category", "school") "web" }
            $level = if ($item -is [string]) { "imported" } else { Get-StringValue $item @("level", "stage", "rank") "imported" }
            $effect = if ($item -is [string]) { "Imported from web save" } else { Get-StringValue $item @("effect", "desc", "description") "Imported from web save" }
            $arts += [ordered]@{
                name = $name
                type = $type
                level = $level
                effect = $effect
            }
        }
    } elseif ($null -ne $source -and !($source -is [string])) {
        foreach ($property in $source.PSObject.Properties) {
            if ($arts.Count -ge 4) {
                break
            }

            $item = $property.Value
            $arts += [ordered]@{
                name = Get-StringValue $item @("name", "title", "id") $property.Name
                type = Get-StringValue $item @("type", "category", "school") "web"
                level = Get-StringValue $item @("level", "stage", "rank") "imported"
                effect = Get-StringValue $item @("effect", "desc", "description") "Imported from web save"
            }
        }
    }

    if ($arts.Count -eq 0) {
        $arts += [ordered]@{
            name = "Imported Inner Skill"
            type = "internal"
            level = "imported"
            effect = "Imported from web save"
        }
    }

    return $arts
}

function Get-Checksum {
    param([string]$Payload)

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes("${saveTextPrefix}:$Payload")
        $hash = $sha.ComputeHash($bytes)
        return ([System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()).Substring(0, 16)
    } finally {
        $sha.Dispose()
    }
}

$inputFullPath = Resolve-ProjectPath $InputPath
$outputFullPath = Resolve-ProjectPath $OutputPath

$raw = [System.IO.File]::ReadAllText($inputFullPath, [System.Text.Encoding]::UTF8).Trim()
if ([string]::IsNullOrWhiteSpace($raw)) {
    throw "Input save is empty: $inputFullPath"
}

if ($raw.StartsWith($saveTextPrefix + "::")) {
    $outputDirectory = Split-Path -Parent $outputFullPath
    if (!(Test-Path -LiteralPath $outputDirectory)) {
        New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    }

    [System.IO.File]::WriteAllText($outputFullPath, $raw, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Input already looks like a Godot save."
    Write-Host "Output: $outputFullPath"
    exit 0
}

if ($raw.StartsWith("{")) {
    $json = $raw
} else {
    $json = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($raw))
}

$web = $json | ConvertFrom-Json
$martialArts = New-MartialArts $web
$weaponKey = Get-StringValue $web @("hero.weapon", "player.weapon", "character.weapon", "rightPanel.weapon.key", "rightPanel.weapon.type", "weapon.key", "weapon.type", "equippedWeaponType") "sword"
$weaponScore = Get-IntValue $web @("rightPanel.weapon.score", "weapon.score", "weapon.power", "weapon.attack", "weaponScore") 197

$godotState = [ordered]@{
    saveSchemaVersion = "v15g.5"
    sourceVersion = "web-bridge-v15g7"
    stage = [ordered]@{
        floor = Get-IntValue $web @("stage.floor", "jianghu.floor", "progress.floor", "floor", "layer") 1
        chapter = Get-StringValue $web @("stage.chapter", "jianghu.chapter", "chapter") "Web Import"
        martialRealm = Get-StringValue $web @("stage.martialRealm", "martialRealm", "realm") "Imported Realm"
        year = Get-IntValue $web @("stage.year", "jianghu.year", "year") 0
        direction = Get-StringValue $web @("stage.direction", "direction", "route") "Imported Route"
        combatPower = Get-StringValue $web @("stage.combatPower", "combatPower", "power", "hero.comprehensivePower") "0"
    }
    hero = [ordered]@{
        name = Get-StringValue $web @("hero.name", "player.name", "character.name", "name") "Imported Hero"
        title = Get-StringValue $web @("hero.title", "player.title", "character.title", "title") "Web Save"
        sect = Get-StringValue $web @("hero.sect", "player.sect", "character.sect", "sect.name", "sect") "Imported Sect"
        realm = Get-StringValue $web @("hero.realm", "player.realm", "character.realm", "realm") "Imported Realm"
        weapon = $weaponKey
        hp = Get-IntValue $web @("hero.hp", "player.hp", "character.hp", "hp", "health") 1
        maxHp = Get-IntValue $web @("hero.maxHp", "player.maxHp", "character.maxHp", "maxHp", "maxHealth") 1
        qi = Get-IntValue $web @("hero.qi", "player.qi", "character.qi", "qi") 100
        comprehensivePower = Get-IntValue $web @("hero.comprehensivePower", "player.comprehensivePower", "character.comprehensivePower", "combatPower", "power") 0
        cultivation = Get-IntValue $web @("hero.cultivation", "player.cultivation", "character.cultivation", "cultivation", "xp") 0
        cultivationPerSecond = Get-DoubleValue $web @("hero.cultivationPerSecond", "player.cultivationPerSecond", "cultivationPerSecond", "xpRate") 0
        effects = Get-StringArrayValue $web @("hero.effects", "player.effects", "effects", "buffs") @("Imported from web save")
        battleStats = [ordered]@{
            outerPower = Get-IntValue $web @("hero.battleStats.outerPower", "stats.outerPower", "outerPower", "attack") 0
            innerPower = Get-IntValue $web @("hero.battleStats.innerPower", "stats.innerPower", "innerPower", "innerAttack") 0
            outerDefense = Get-IntValue $web @("hero.battleStats.outerDefense", "stats.outerDefense", "outerDefense", "defense") 0
            innerDefense = Get-IntValue $web @("hero.battleStats.innerDefense", "stats.innerDefense", "innerDefense", "innerDefenseValue") 0
            critRate = Get-IntValue $web @("hero.battleStats.critRate", "stats.critRate", "critRate") 0
            dodgeRate = Get-IntValue $web @("hero.battleStats.dodgeRate", "stats.dodgeRate", "dodgeRate") 0
        }
        resources = [ordered]@{
            silver = Get-IntValue $web @("hero.resources.silver", "resources.silver", "silver", "money") 0
            essence = Get-IntValue $web @("hero.resources.essence", "resources.essence", "essence") 0
            food = Get-IntValue $web @("hero.resources.food", "resources.food", "food") 0
        }
        jianghuQualities = [ordered]@{
            reputation = Get-IntValue $web @("hero.jianghuQualities.reputation", "qualities.reputation", "reputation", "fame") 0
            comprehension = Get-IntValue $web @("hero.jianghuQualities.comprehension", "qualities.comprehension", "comprehension", "wit") 0
            luck = Get-IntValue $web @("hero.jianghuQualities.luck", "qualities.luck", "luck", "fortune") 0
            charm = Get-IntValue $web @("hero.jianghuQualities.charm", "qualities.charm", "charm") 0
            root = Get-IntValue $web @("hero.jianghuQualities.root", "qualities.root", "root") 0
            resolve = Get-IntValue $web @("hero.jianghuQualities.resolve", "qualities.resolve", "resolve", "will") 0
        }
    }
    enemy = [ordered]@{
        name = Get-StringValue $web @("enemy.name", "opponent.name") "Imported Opponent"
        rank = Get-StringValue $web @("enemy.rank", "opponent.rank") "web"
        tag = Get-StringValue $web @("enemy.tag", "opponent.tag") "web"
        hp = Get-IntValue $web @("enemy.hp", "opponent.hp") 1
        maxHp = Get-IntValue $web @("enemy.maxHp", "opponent.maxHp") 1
        combatPower = Get-StringValue $web @("enemy.combatPower", "opponent.combatPower") "0"
    }
    sectBonuses = Get-StringArrayValue $web @("sectBonuses", "rightPanel.sect.bonuses", "sect.bonuses", "bonuses") @("Imported sect bonus")
    martialArts = $martialArts
    romance = [ordered]@{
        name = Get-StringValue $web @("romance.name", "spouse.name", "partner.name") "Imported Partner"
        favor = Get-IntValue $web @("romance.favor", "spouse.favor", "partner.favor", "favor") 0
        status = Get-StringValue $web @("romance.status", "spouse.status", "partner.status") "Imported"
        badge = Get-StringValue $web @("romance.badge", "spouse.badge", "partner.badge") "web"
    }
    weaponInfo = [ordered]@{
        title = "Weapon"
        name = Get-StringValue $web @("rightPanel.weapon.name", "weapon.name", "equippedWeapon.name") "Imported Weapon"
        type = Get-StringValue $web @("rightPanel.weapon.type", "weapon.type", "equippedWeapon.type", "hero.weapon") $weaponKey
        score = $weaponScore
        socket = Get-StringValue $web @("rightPanel.weapon.socket", "weapon.socket") "Godot WeaponSocket"
        effects = Get-StringArrayValue $web @("rightPanel.weapon.effects", "weapon.effects", "equippedWeapon.effects") @("Imported weapon score")
    }
    commands = [ordered]@{
        isPaused = $true
        autoExplore = $false
        activeMartialArtIndex = 0
        lastCommand = "Imported web save"
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
        eventLog = @("Imported web save")
        events = @(
            [ordered]@{
                turn = 0
                kind = "import"
                actor = "web"
                target = "godot"
                amount = 0
                message = "Imported web save"
            }
        )
        migrationWarnings = @(
            "Web save bridge is field-mapped in V15G.7.",
            "Run tools/analyze-web-save-fields.ps1 for unmapped fields."
        )
    }
}

$payloadJson = $godotState | ConvertTo-Json -Depth 16 -Compress
$payload = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($payloadJson))
$checksum = Get-Checksum $payload
$saveText = "${saveTextPrefix}::$payload::$checksum"

$outputDirectory = Split-Path -Parent $outputFullPath
if (!(Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

[System.IO.File]::WriteAllText($outputFullPath, $saveText, [System.Text.UTF8Encoding]::new($false))

Write-Host "Converted web save to Godot save text."
Write-Host "Input: $inputFullPath"
Write-Host "Output: $outputFullPath"
