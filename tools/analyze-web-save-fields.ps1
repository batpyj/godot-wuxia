param(
    [string]$InputPath = "data/combat-sample.json",
    [string]$OutputPath = "tools/generated/web-save-field-report.json"
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

function Get-InputJson {
    param([string]$Path)

    $raw = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8).Trim()
    if ([string]::IsNullOrWhiteSpace($raw)) {
        throw "Input save is empty: $Path"
    }

    if ($raw.StartsWith($saveTextPrefix + "::")) {
        $parts = $raw.Split("::")
        if ($parts.Count -lt 3) {
            throw "Invalid Godot save text: $Path"
        }

        return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($parts[1]))
    }

    if ($raw.StartsWith("{")) {
        return $raw
    }

    return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($raw))
}

function Add-FlatJsonPaths {
    param(
        [object]$Value,
        [string]$Prefix,
        [System.Collections.Generic.List[string]]$Paths
    )

    if ($null -eq $Value) {
        if (![string]::IsNullOrWhiteSpace($Prefix)) {
            $Paths.Add($Prefix)
        }

        return
    }

    if ($Value -is [string] -or $Value -is [ValueType]) {
        if (![string]::IsNullOrWhiteSpace($Prefix)) {
            $Paths.Add($Prefix)
        }

        return
    }

    if ($Value -is [System.Collections.IDictionary]) {
        foreach ($key in $Value.Keys) {
            $child = if ([string]::IsNullOrWhiteSpace($Prefix)) { [string]$key } else { "$Prefix.$key" }
            Add-FlatJsonPaths -Value $Value[$key] -Prefix $child -Paths $Paths
        }

        return
    }

    if ($Value -is [System.Collections.IEnumerable] -and !($Value -is [string])) {
        $index = 0
        foreach ($item in $Value) {
            $child = if ([string]::IsNullOrWhiteSpace($Prefix)) { "[$index]" } else { "$Prefix[$index]" }
            Add-FlatJsonPaths -Value $item -Prefix $child -Paths $Paths
            $index += 1
        }

        if ($index -eq 0 -and ![string]::IsNullOrWhiteSpace($Prefix)) {
            $Paths.Add($Prefix)
        }

        return
    }

    $properties = $Value.PSObject.Properties
    if ($properties.Count -eq 0) {
        if (![string]::IsNullOrWhiteSpace($Prefix)) {
            $Paths.Add($Prefix)
        }

        return
    }

    foreach ($property in $properties) {
        $child = if ([string]::IsNullOrWhiteSpace($Prefix)) { $property.Name } else { "$Prefix.$($property.Name)" }
        Add-FlatJsonPaths -Value $property.Value -Prefix $child -Paths $Paths
    }
}

function Test-MappedPath {
    param(
        [string]$Path,
        [string[]]$KnownPrefixes
    )

    foreach ($prefix in $KnownPrefixes) {
        $prefixDot = $prefix + "."
        $prefixArray = $prefix + "["
        $isExact = $Path -eq $prefix
        $isNestedObject = $Path.StartsWith($prefixDot, [System.StringComparison]::Ordinal)
        $isNestedArray = $Path.StartsWith($prefixArray, [System.StringComparison]::Ordinal)
        if ($isExact -or $isNestedObject -or $isNestedArray) {
            return $true
        }
    }

    return $false
}

function Get-TopLevel {
    param([string]$Path)

    $dot = $Path.IndexOf(".")
    $array = $Path.IndexOf("[")
    $end = $Path.Length

    if ($dot -ge 0) {
        $end = [Math]::Min($end, $dot)
    }

    if ($array -ge 0) {
        $end = [Math]::Min($end, $array)
    }

    return $Path.Substring(0, $end)
}

$knownPrefixes = @(
    "stage",
    "jianghu",
    "progress",
    "floor",
    "layer",
    "chapter",
    "martialRealm",
    "realm",
    "direction",
    "route",
    "combatPower",
    "power",
    "hero",
    "player",
    "character",
    "name",
    "title",
    "sect",
    "hp",
    "health",
    "maxHp",
    "maxHealth",
    "qi",
    "cultivation",
    "cultivationPerSecond",
    "xp",
    "xpRate",
    "effects",
    "buffs",
    "battleStats",
    "stats",
    "outerPower",
    "innerPower",
    "outerDefense",
    "innerDefense",
    "critRate",
    "dodgeRate",
    "resources",
    "silver",
    "money",
    "essence",
    "food",
    "jianghuQualities",
    "qualities",
    "reputation",
    "fame",
    "comprehension",
    "wit",
    "luck",
    "fortune",
    "charm",
    "root",
    "resolve",
    "enemy",
    "opponent",
    "sectBonuses",
    "bonuses",
    "martialArts",
    "rightPanel.martialArts",
    "skills",
    "manuals",
    "arts",
    "kungfu",
    "romance",
    "spouse",
    "partner",
    "favor",
    "weapon",
    "weaponInfo",
    "rightPanel.weapon",
    "equippedWeapon",
    "equippedWeaponType",
    "weaponScore",
    "commands",
    "combat"
)

$inputFullPath = Resolve-ProjectPath $InputPath
$outputFullPath = Resolve-ProjectPath $OutputPath
$json = Get-InputJson -Path $inputFullPath
$save = $json | ConvertFrom-Json

$paths = [System.Collections.Generic.List[string]]::new()
Add-FlatJsonPaths -Value $save -Prefix "" -Paths $paths

$mapped = @()
$unmapped = @()
foreach ($path in ($paths | Sort-Object -Unique)) {
    if (Test-MappedPath -Path $path -KnownPrefixes $knownPrefixes) {
        $mapped += $path
    } else {
        $unmapped += $path
    }
}

$report = [ordered]@{
    source = $inputFullPath
    output = $outputFullPath
    totalPaths = $paths.Count
    mappedCount = $mapped.Count
    unmappedCount = $unmapped.Count
    mappedTopLevel = @($mapped | ForEach-Object { Get-TopLevel $_ } | Sort-Object -Unique)
    unmappedTopLevel = @($unmapped | ForEach-Object { Get-TopLevel $_ } | Sort-Object -Unique)
    mappedPaths = $mapped
    unmappedPaths = $unmapped
    knownPrefixes = $knownPrefixes
    notes = @(
        "Mapped means the current bridge recognizes the path directly or through a supported prefix.",
        "Unmapped fields are kept in this report only; convert-web-save.ps1 does not import them yet.",
        "The save text envelope stays WUXIA_SAVE_V15G0 while the stable payload schema is v15g.5."
    )
}

$outputDirectory = Split-Path -Parent $outputFullPath
if (!(Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$reportJson = $report | ConvertTo-Json -Depth 12
[System.IO.File]::WriteAllText($outputFullPath, $reportJson, [System.Text.UTF8Encoding]::new($false))

Write-Host "Web save field report written."
Write-Host "Input: $inputFullPath"
Write-Host "Output: $outputFullPath"
Write-Host "Mapped paths: $($mapped.Count)"
Write-Host "Unmapped paths: $($unmapped.Count)"
