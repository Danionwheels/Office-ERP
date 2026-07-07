param(
    [switch]$SkipApiBuilds,
    [switch]$SkipFrontend,
    [switch]$SkipSmokes
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$verifyRoot = Join-Path $repoRoot ".codex-run\verify"

function Invoke-VerifyStep {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [scriptblock]$Script
    )

    Write-Host ""
    Write-Host "==> $Name"
    $startedAt = Get-Date

    & $Script

    $elapsed = (Get-Date) - $startedAt
    Write-Host ("OK: {0} ({1:n1}s)" -f $Name, $elapsed.TotalSeconds)
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw ("{0} exited with code {1}." -f $FilePath, $LASTEXITCODE)
    }
}

function Invoke-DotNetBuildToVerifyOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Project,

        [Parameter(Mandatory = $true)]
        [string]$OutputName
    )

    $outputPath = Join-Path $verifyRoot $OutputName
    Invoke-NativeCommand `
        -FilePath "dotnet" `
        -Arguments @("build", $Project, "--no-restore", "-p:OutDir=$outputPath\")
}

Push-Location $repoRoot
try {
    New-Item -ItemType Directory -Force -Path $verifyRoot | Out-Null

    if (-not $SkipApiBuilds) {
        Invoke-VerifyStep "Build Control Desk API" {
            Invoke-DotNetBuildToVerifyOutput `
                -Project "src\SafarSuite.ControlDesk.Api\SafarSuite.ControlDesk.Api.csproj" `
                -OutputName "control-desk-api"
        }

        Invoke-VerifyStep "Build Control Cloud API" {
            Invoke-DotNetBuildToVerifyOutput `
                -Project "src\SafarSuite.ControlCloud.Api\SafarSuite.ControlCloud.Api.csproj" `
                -OutputName "control-cloud-api"
        }

        Invoke-VerifyStep "Build LocalServer API" {
            Invoke-DotNetBuildToVerifyOutput `
                -Project "src\SafarSuite.LocalServer.Api\SafarSuite.LocalServer.Api.csproj" `
                -OutputName "local-server-api"
        }
    }

    if (-not $SkipFrontend) {
        Invoke-VerifyStep "Build Control Desk UI" {
            Push-Location "apps\control-desk-ui"
            try {
                Invoke-NativeCommand -FilePath "npm" -Arguments @("run", "build")
            }
            finally {
                Remove-Item -LiteralPath "tsconfig.tsbuildinfo", "tsconfig.node.tsbuildinfo" -Force -ErrorAction SilentlyContinue
                Pop-Location
            }
        }
    }

    if (-not $SkipSmokes) {
        Invoke-VerifyStep "Run accounting smoke" {
            Invoke-NativeCommand `
                -FilePath "dotnet" `
                -Arguments @(
                    "run",
                    "--project",
                    "tools\SafarSuite.ControlDesk.AccountingSmoke\SafarSuite.ControlDesk.AccountingSmoke.csproj",
                    "--no-restore",
                    "--",
                    "--provider",
                    "inmemory"
                )
        }

        Invoke-VerifyStep "Run LocalServer entitlement smoke" {
            Invoke-NativeCommand `
                -FilePath "dotnet" `
                -Arguments @(
                    "run",
                    "--project",
                    "tools\SafarSuite.LocalServer.EntitlementSmoke\SafarSuite.LocalServer.EntitlementSmoke.csproj",
                    "--no-restore"
                )
        }
    }

    Write-Host ""
    Write-Host "Local verification passed."
}
finally {
    Pop-Location
}
