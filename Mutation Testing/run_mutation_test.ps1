param
(
    [Parameter(Mandatory = $false)]
    [switch]$Visualize,

    [Parameter(Mandatory = $false)]
    [switch]$Rerun
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BaseDir = Split-Path -Parent $ScriptDir

$OriginalTestProject = Join-Path $BaseDir "Mzinga\src\Mzinga.Test\Mzinga.Test.csproj"
$NewTestProject = Join-Path $BaseDir "Unit Tests\Mzinga.Tests.New\Mzinga.Tests.New.csproj"
$ResultsDir = Join-Path $ScriptDir "Results"

Write-Host "Checking Stryker CLI availability...`n" -ForegroundColor Green
$strykerInstalled = dotnet tool list -g | Select-String "dotnet-stryker"
if (-not $strykerInstalled)
{
    Write-Host "Stryker not found. Installing dotnet-stryker globally..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-stryker
}

Write-Host "Starting Mutation Testing...`n" -ForegroundColor Green

$StrykerDefaultOutput = Join-Path $BaseDir "StrykerOutput"

if (-not (Test-Path $ResultsDir))
{
    New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
}

try
{
    $existingReports = Get-ChildItem -Path "$ResultsDir\*\reports\mutation-report.html" -ErrorAction SilentlyContinue | Sort-Object CreationTime -Descending
    $latestReportPath = if ($existingReports) { $existingReports[0].FullName } else { $null }

    if ($latestReportPath -and -not $Rerun)
    {
        Write-Host "Mutation testing results already exist in $ResultsDir. Skipping execution. Use -Rerun to force re-execution.`n" -ForegroundColor Cyan
        if ($Visualize)
        {
            Write-Host "Opening existing report...`n" -ForegroundColor Green
            Start-Process $latestReportPath
        }
    }
    else
    {
        if ($Rerun -and $latestReportPath)
        {
            Write-Host "Cleaning previous results...`n" -ForegroundColor Green
            Remove-Item -Path $ResultsDir\* -Recurse -Force -ErrorAction SilentlyContinue
        }

        Push-Location $BaseDir
        try
        {
            $strykerArgs = @()

            $strykerArgs += "--project"
            $strykerArgs += "Mzinga.csproj"
            
            $strykerArgs += "--test-project"
            $strykerArgs += $OriginalTestProject
            
            $strykerArgs += "--test-project"
            $strykerArgs += $NewTestProject

            $strykerArgs += "--mutate"
            $strykerArgs += "**/Engine.cs"
            $strykerArgs += "--mutate"
            $strykerArgs += "**/EngineConfig.cs"

            $MaxConcurrency = [Math]::Max(1, [System.Environment]::ProcessorCount - 1)
            $strykerArgs += "--concurrency"
            $strykerArgs += $MaxConcurrency

            if ($Visualize)
            {
                $strykerArgs += "-o"
            }
            
            & dotnet stryker @strykerArgs
        }
        finally
        {
            Pop-Location
        }

        if (Test-Path $StrykerDefaultOutput)
        {
            Move-Item -Path "$StrykerDefaultOutput\*" -Destination $ResultsDir -Force
            Remove-Item -Path $StrykerDefaultOutput -Recurse -Force
        }
    }
}
catch
{
    Write-Host "Stryker encountered an error.`n" -ForegroundColor Yellow
}

Write-Host "=============================================" -ForegroundColor Green
Write-Host " Mutation testing complete!" -ForegroundColor Green
Write-Host " Check '$ResultsDir' for HTML report." -ForegroundColor Green
Write-Host "=============================================`n" -ForegroundColor Green
