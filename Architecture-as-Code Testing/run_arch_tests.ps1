param
(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$TargetDir,

    [Parameter(Mandatory = $false)]
    [switch]$Visualize
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionDir = "$ScriptDir\Mzinga.Architecture.Tests"
$ArchTestProj = "$SolutionDir\Mzinga.Architecture.Tests.csproj"

$ResultsDir = Join-Path $ScriptDir "Results"
$TargetResultsDir = Join-Path $ResultsDir $TargetDir
$TestResultsPath = Join-Path $TargetResultsDir "TestResults.trx"
$HtmlReportPath = Join-Path $TargetResultsDir "Report.html"

if (Test-Path $TargetResultsDir)
{
    Remove-Item -Path $TargetResultsDir -Recurse -Force -ErrorAction SilentlyContinue
}
$null = New-Item -ItemType Directory -Path $TargetResultsDir -Force

Write-Host "=============================================" -ForegroundColor Green
Write-Host "Running Architecture tests for Mzinga..." -ForegroundColor Green
Write-Host "=============================================`n" -ForegroundColor Green

dotnet test $ArchTestProj --logger "trx;LogFileName=$TestResultsPath"

if ($Visualize)
{
    if (!(Get-Command trxlog2html -ErrorAction SilentlyContinue))
    {
        Write-Host "Installing trxlog2html globaltool...`n" -ForegroundColor Green
        dotnet tool install -g trxlog2html --ignore-failed-sources 2>&1 | Out-Null
    }

    Write-Host "=============================================" -ForegroundColor Green
    Write-Host "Generating HTML report..." -ForegroundColor Green
    Write-Host "=============================================`n" -ForegroundColor Green

    $HtmlIndex = Join-Path $TargetResultsDir "Report.html"
    trxlog2html -i $TestResultsPath -o $HtmlIndex

    if (Test-Path $HtmlIndex)
    {
        Start-Process $HtmlIndex
    }
}
