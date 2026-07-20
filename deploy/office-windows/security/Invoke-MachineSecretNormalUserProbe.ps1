param(
    [Parameter(Mandatory = $true)]
    [string]$EnvelopePath,

    [Parameter(Mandatory = $true)]
    [string]$ResultPath,

    [Parameter(Mandatory = $true)]
    [string]$ReplacementPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Test-Denied {
    param([Parameter(Mandatory = $true)][scriptblock]$Operation)

    try {
        & $Operation
        return $false
    }
    catch [System.UnauthorizedAccessException] {
        return $true
    }
    catch [System.IO.IOException] {
        return $true
    }
}

$readDenied = Test-Denied { [void][IO.File]::ReadAllBytes($EnvelopePath) }
$writeDenied = Test-Denied { [IO.File]::WriteAllText($EnvelopePath, 'normal-user-write') }
$deleteDenied = Test-Denied { [IO.File]::Delete($EnvelopePath) }
$replaceDenied = Test-Denied { Move-Item -LiteralPath $ReplacementPath -Destination $EnvelopePath -Force }

[ordered]@{
    readDenied = $readDenied
    writeDenied = $writeDenied
    deleteDenied = $deleteDenied
    replaceDenied = $replaceDenied
} | ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding utf8
