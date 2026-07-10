param(
    [string]$InputPath = "data/combat-sample.json",
    [string]$ReportPath = "tools/generated/web-save-field-report.json"
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$reportFullPath = Join-Path $root $ReportPath

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (!$Condition) {
        throw $Message
    }
}

if (Test-Path -LiteralPath $reportFullPath) {
    Remove-Item -LiteralPath $reportFullPath -Force
}

powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "analyze-web-save-fields.ps1") -InputPath $InputPath -OutputPath $ReportPath

Assert-True (Test-Path -LiteralPath $reportFullPath) "Field report was not created: $reportFullPath"

$report = Get-Content -LiteralPath $reportFullPath -Raw | ConvertFrom-Json
$mappedTopLevel = @($report.mappedTopLevel)

Assert-True ($report.totalPaths -gt 0) "Field report did not find any JSON paths."
Assert-True ($report.mappedCount -gt 0) "Field report did not map any JSON paths."
Assert-True ($mappedTopLevel -contains "stage") "Field report did not map stage fields."
Assert-True ($mappedTopLevel -contains "hero") "Field report did not map hero fields."
Assert-True ($mappedTopLevel -contains "enemy") "Field report did not map enemy fields."
Assert-True ($mappedTopLevel -contains "rightPanel") "Field report did not map rightPanel fields."

Write-Host "Web save field report assertions passed."
Write-Host "Report: $reportFullPath"
