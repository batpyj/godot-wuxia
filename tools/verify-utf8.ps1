$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true)
$textExtensions = @(
    ".cs", ".csproj", ".sln", ".props", ".targets",
    ".godot", ".tscn", ".tres", ".gd",
    ".json", ".md", ".ps1", ".import", ".cfg",
    ".xml", ".yml", ".yaml", ".csv", ".txt",
    ".editorconfig", ".gitattributes", ".gitignore", ".gdignore"
)

$failures = New-Object System.Collections.Generic.List[string]
$checked = 0

function Get-ProjectRelativePath {
    param([string]$Path)

    if ($Path.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Path.Substring($root.Length).TrimStart('\', '/')
    }

    return $Path
}

Get-ChildItem -LiteralPath $root -Recurse -File -Force |
    Where-Object {
        $relative = Get-ProjectRelativePath $_.FullName
        $top = ($relative -split [Regex]::Escape([System.IO.Path]::DirectorySeparatorChar))[0]
        $top -notin @(".git", ".godot", ".agents", "bin", "obj") -and
            ($textExtensions -contains $_.Extension -or $textExtensions -contains $_.Name)
    } |
    ForEach-Object {
        $checked += 1
        $bytes = [System.IO.File]::ReadAllBytes($_.FullName)

        if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
            $failures.Add("BOM: $(Get-ProjectRelativePath $_.FullName)")
            return
        }

        try {
            [void]$utf8Strict.GetString($bytes)
        } catch {
            $failures.Add("Invalid UTF-8: $(Get-ProjectRelativePath $_.FullName)")
        }
    }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "UTF-8 check passed: $checked text files."
