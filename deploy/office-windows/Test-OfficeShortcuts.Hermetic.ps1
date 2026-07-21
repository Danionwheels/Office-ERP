$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Install-OfficeShortcuts.ps1')
$checks = [ordered]@{
    elevation = $source.Contains('WindowsBuiltInRole]::Administrator')
    launcherTarget = $source.Contains('Start-OfficeControlDesk.ps1')
    startMenu = $source.Contains('CommonPrograms')
    optionalDesktop = $source.Contains('DesktopShortcut')
    idempotent = $source.Contains('Create or repair owned shortcut')
    powershell = $source.Contains('powershell.exe')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "Shortcut contract check failed: $($check.Key)" } }
Write-Host "Control Desk shortcut hermetic contract: passed ($($checks.Count) checks)"
