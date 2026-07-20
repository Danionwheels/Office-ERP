$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Uninstall-OfficeControlDesk.ps1')
$checks = [ordered]@{
    elevation = $source.Contains('WindowsBuiltInRole]::Administrator')
    receiptRequired = $source.Contains('no ownership receipt')
    foreignRefusal = $source.Contains('Foreign API service detected')
    serviceRemoval = $source.Contains('sc.exe delete')
    shortcutRemoval = $source.Contains('CommonPrograms') -and $source.Contains('CommonDesktopDirectory')
    payloadRemoval = $source.Contains("'Api')) { Remove-Item")
    dataPreserved = $source.Contains('machine secrets') -and $source.Contains('were preserved')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "Uninstall contract check failed: $($check.Key)" } }
Write-Host "Control Desk uninstall hermetic contract: passed ($($checks.Count) checks)"
