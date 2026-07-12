param
(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("check", "apply")]
    [string]$Mode,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$TargetDir,

    [Parameter(Mandatory = $false)]
    [switch]$Visualize
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BaseDir = Split-Path -Parent $ScriptDir

$SolutionPath = Join-Path $BaseDir "Mzinga\src\Mzinga.sln"
$MzingaProject = Join-Path $BaseDir "Mzinga\src\Mzinga\Mzinga.csproj"
$MzingaEngineProject = Join-Path $BaseDir "Mzinga\src\Mzinga.Engine\Mzinga.Engine.csproj"
$ResultsDir = Join-Path $ScriptDir "Results"
$TargetResultsDir = Join-Path $ResultsDir $TargetDir

Write-Host "Adding Roslynator packages to Mzinga...`n" -ForegroundColor Green
dotnet add $MzingaProject package Roslynator.Analyzers > $null 2>&1
dotnet add $MzingaEngineProject package Roslynator.Analyzers > $null 2>&1

Write-Host "Cleaning previous results for $Mode mode...`n" -ForegroundColor Green
if (Test-Path $TargetResultsDir) { Remove-Item -Path $TargetResultsDir -Recurse -Force -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Path $TargetResultsDir -Force | Out-Null

Write-Host "=============================================" -ForegroundColor Green
Write-Host "Running Roslynator ($Mode) on Mzinga project" -ForegroundColor Green
Write-Host "=============================================`n" -ForegroundColor Green

if ($Mode -eq "check")
{
    dotnet format analyzers $SolutionPath --verify-no-changes --report $TargetResultsDir
}
else
{
    dotnet format analyzers $SolutionPath --report $TargetResultsDir
}

if ($Visualize)
{
    Write-Host "`n=============================================" -ForegroundColor Green
    Write-Host "Visualizing Roslynator report" -ForegroundColor Green
    Write-Host "=============================================`n" -ForegroundColor Green
    
    $JsonFiles = @(Get-ChildItem -Path $TargetResultsDir -Filter "*.json" -Recurse)
    
    if ($JsonFiles.Count -gt 0)
    {
        $HtmlReportPath = Join-Path $TargetResultsDir "index.html"
        $Html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Roslynator Report - $Mode</title>
    <style>
        body { font-family: sans-serif; padding: 20px; }
        table { border-collapse: collapse; width: 100%; margin-top: 20px; }
        th, td { border: 1px solid #dddddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; }
    </style>
</head>
<body>
    <h1>Roslynator Static Analysis Report</h1>
    <h2>Mode: <span style="color: $(if($Mode -eq 'check') {'#d9534f'} else {'#5cb85c'})">$Mode</span></h2>
    <table>
        <tr>
            <th>File</th>
            <th>Rule ID</th>
            <th>Line</th>
            <th>Description</th>
        </tr>
"@
        $HasIssues = $false
        foreach ($JsonFile in $JsonFiles)
        {
            $JsonContent = Get-Content $JsonFile.FullName -Raw | ConvertFrom-Json
            foreach ($FileReport in $JsonContent)
            {
                $FileName = Split-Path $FileReport.FilePath -Leaf
                foreach ($Change in $FileReport.FileChanges)
                {
                    $HasIssues = $true
                    $Html += "<tr><td>$FileName</td><td>$($Change.DiagnosticId)</td><td>$($Change.LineNumber)</td><td>$($Change.FormatDescription)</td></tr>"
                }
            }
        }
        
        $Html += "</table>"
        if (-not $HasIssues) { $Html += "<p>No analyzer issues found!</p>" }
        $Html += "</body></html>"
        
        Set-Content -Path $HtmlReportPath -Value $Html
        
        Write-Host "Opening report in browser...`n" -ForegroundColor Green
        Start-Process $HtmlReportPath
    }
    else
    {
        Write-Host "No JSON reports found.`n" -ForegroundColor Yellow
    }
}
