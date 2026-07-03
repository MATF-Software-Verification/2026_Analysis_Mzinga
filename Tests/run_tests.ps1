param
(
    [Parameter(Mandatory = $false, Position = 0)]
    [ValidateSet("original", "new", "all")]
    [string]$Target = "all",

    [Parameter(Mandatory = $false)]
    [switch]$Visualize
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BaseDir = Split-Path -Parent $ScriptDir

$TestsDir = $ScriptDir
$OriginalTestProject = Join-Path $BaseDir "Mzinga\src\Mzinga.Test\Mzinga.Test.csproj"
$ResultsDir = Join-Path $TestsDir "Results"
$CoverageReportDir = Join-Path $TestsDir "CoverageReport"

if (!(Test-Path $ResultsDir))
{
    New-Item -ItemType Directory -Path $ResultsDir | Out-Null
}
else
{
    Remove-Item -Path (Join-Path $ResultsDir "*") -Recurse -Force -ErrorAction SilentlyContinue
}

function Run-DotnetTest
{
    param([string]$ProjectPath)
    if (Test-Path $ProjectPath)
    {
        Write-Host "=============================================" -ForegroundColor Cyan
        Write-Host "Running tests at: $ProjectPath" -ForegroundColor Cyan
        Write-Host "=============================================" -ForegroundColor Cyan
        
        dotnet test $ProjectPath --collect:"XPlat Code Coverage" --results-directory $ResultsDir
    }
    else
    {
        Write-Host "Project not found: $ProjectPath" -ForegroundColor Yellow
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

if ($Visualize)
{
    Write-Host ""
    Write-Host "=============================================" -ForegroundColor Green
    Write-Host "Generating coverage report visualization" -ForegroundColor Green
    Write-Host "=============================================" -ForegroundColor Green
    
    if (!(Get-Command reportgenerator -ErrorAction SilentlyContinue))
    {
        Write-Host "Installing dotnet-reportgenerator-globaltool..."
        dotnet tool install -g dotnet-reportgenerator-globaltool --ignore-failed-sources -ErrorAction SilentlyContinue
    }
    
    $ReportsPattern = Join-Path $ResultsDir "*\coverage.cobertura.xml"
    
    if (Test-Path (Join-Path $ResultsDir "*\coverage.cobertura.xml"))
    {
        reportgenerator -reports:$ReportsPattern -targetdir:$CoverageReportDir -reporttypes:Html
        
        $IndexFile = Join-Path $CoverageReportDir "index.html"
        if (Test-Path $IndexFile)
        {
            Write-Host "Opening coverage report in browser..." -ForegroundColor Green
            Start-Process $IndexFile
        }
        else
        {
            Write-Host "Coverage report not found." -ForegroundColor Red
        }
    }
    else
    {
        Write-Host "coverage.cobertura.xml files not found. Check if reports were generated." -ForegroundColor Yellow
    }
}
