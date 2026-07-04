param
(
    [Parameter(Mandatory = $false, Position = 0)]
    [ValidateSet("original", "new", "all")]
    [string]$Target = "all",

    [Parameter(Mandatory = $false)]
    [switch]$Visualize,

    [Parameter(Mandatory = $false)]
    [switch]$Rerun,

    [Parameter(Mandatory = $false)]
    [switch]$Clean
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BaseDir = Split-Path -Parent $ScriptDir

$TestsDir = Join-Path $BaseDir "Tests\Mzinga.Tests.New"
$OriginalTestProject = Join-Path $BaseDir "Mzinga\src\Mzinga.Test\Mzinga.Test.csproj"
$ResultsDir = Join-Path $ScriptDir "Results"
$CoverageReportDir = Join-Path $ScriptDir "CoverageReport"

if ($Clean)
{
    Write-Host "Cleaning results and coverage reports...`n" -ForegroundColor Green
    if (Test-Path $ResultsDir) { Remove-Item -Path $ResultsDir -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path $CoverageReportDir) { Remove-Item -Path $CoverageReportDir -Recurse -Force -ErrorAction SilentlyContinue }
    Write-Host "Clean complete.`n" -ForegroundColor Green
    exit
}

$TargetResultsDir = Join-Path $ResultsDir $Target
$TargetCoverageReportDir = Join-Path $CoverageReportDir $Target

$NeedsRun = $true

if (Test-Path $TargetResultsDir)
{
    if ($Rerun)
    {
        Write-Host "Cleaning previous results for $Target tests`n" -ForegroundColor Green
        Remove-Item -Path $TargetResultsDir -Recurse -Force -ErrorAction SilentlyContinue
        if (Test-Path $TargetCoverageReportDir) { Remove-Item -Path $TargetCoverageReportDir -Recurse -Force -ErrorAction SilentlyContinue }
    }
    else
    {
        Write-Host "Results for $Target tests already exist. Skipping test execution. Use -Rerun to force re-execution.`n" -ForegroundColor Cyan
        $NeedsRun = $false
    }
}

if ($NeedsRun)
{
    New-Item -ItemType Directory -Path $TargetResultsDir -Force | Out-Null
    
    function Run-DotnetTest
    {
        param([string]$ProjectPath)
        if (Test-Path $ProjectPath)
        {
            Write-Host "=============================================" -ForegroundColor Green
            Write-Host "Running tests at: $ProjectPath" -ForegroundColor Green
            Write-Host "=============================================`n" -ForegroundColor Green
            
            dotnet test $ProjectPath --collect:"XPlat Code Coverage" --results-directory $TargetResultsDir
        }
        else
        {
            Write-Host "Project not found: $ProjectPath`n" -ForegroundColor Yellow
        }
    }

    if ($Target -eq "original" -or $Target -eq "all")
    {
        Run-DotnetTest -ProjectPath $OriginalTestProject
    }

    if ($Target -eq "new" -or $Target -eq "all")
    {
        Run-DotnetTest -ProjectPath $TestsDir
    }
}

if ($Visualize)
{
    Write-Host "=============================================" -ForegroundColor Green
    Write-Host "Generating coverage report visualization for $Target tests" -ForegroundColor Green
    Write-Host "=============================================`n" -ForegroundColor Green
    
    if (!(Get-Command reportgenerator -ErrorAction SilentlyContinue))
    {
        Write-Host "Installing dotnet-reportgenerator-globaltool...`n" -ForegroundColor Green
        dotnet tool install -g dotnet-reportgenerator-globaltool --ignore-failed-sources 2>&1 | Out-Null
    }
    
    $CoverageFiles = @(Get-ChildItem -Path $TargetResultsDir -Filter "coverage.cobertura.xml" -Recurse)
    
    if ($CoverageFiles.Count -gt 0)
    {
        $ReportsPattern = Join-Path $TargetResultsDir "**\coverage.cobertura.xml"
        
        reportgenerator -reports:"$ReportsPattern" -targetdir:"$TargetCoverageReportDir" -reporttypes:Html 2>&1 | Out-Null
        
        $IndexFile = Join-Path $TargetCoverageReportDir "index.html"
        if (Test-Path $IndexFile)
        {
            Write-Host "Opening coverage report in browser...`n" -ForegroundColor Green
            Start-Process $IndexFile
        }
        else
        {
            Write-Host "Coverage report not found.`n" -ForegroundColor Yellow
        }
    }
    else
    {
        Write-Host "coverage.cobertura.xml files not found for $Target tests. Check if reports were generated.`n" -ForegroundColor Yellow
    }
}
