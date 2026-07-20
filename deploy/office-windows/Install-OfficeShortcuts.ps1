[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)] [string]$InstallRoot,
    [switch]$DesktopShortcut
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Shortcut installation requires Windows.' }
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Shortcut installation requires administrator elevation.' }

$launcherPath = Join-Path $InstallRoot 'Launcher\Start-OfficeControlDesk.ps1'
if (-not (Test-Path -LiteralPath $launcherPath -PathType Leaf)) { throw 'The readiness-aware launcher is missing.' }
$shell = New-Object -ComObject WScript.Shell
$startMenu = Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'SafarSuite Control Desk.lnk'
$targets = @($startMenu)
if ($DesktopShortcut) { $targets += Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'SafarSuite Control Desk.lnk' }

foreach ($shortcutPath in $targets) {
    if ($PSCmdlet.ShouldProcess($shortcutPath, 'Create or repair owned shortcut')) {
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe')
        $shortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$launcherPath`""
        $shortcut.WorkingDirectory = Split-Path -Parent $launcherPath
        $shortcut.Description = 'SafarSuite Control Desk readiness-aware launcher'
        $shortcut.Save()
    }
}

Write-Output "Control Desk shortcut contract verified for $($targets.Count) shortcut(s)."
