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

$MzingaSrcDir = Join-Path $BaseDir "Mzinga\src"
$SlnPath = Join-Path $MzingaSrcDir "Mzinga.sln"
$ResultsDir = Join-Path $ScriptDir "Results"

if (-not [string]::IsNullOrEmpty($TargetDir))
{
    $ResultsDir = Join-Path $ResultsDir $TargetDir
}

$CustomConfigPath = Join-Path $ScriptDir ".editorconfig"
$TargetConfigPath = Join-Path $MzingaSrcDir ".editorconfig"

if (-not (Test-Path $ResultsDir))
{
    New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
}

$ReportOutputDir = Join-Path $ResultsDir "Report_$Mode"

if (Test-Path $CustomConfigPath)
{
    Copy-Item -Path $CustomConfigPath -Destination $TargetConfigPath -Force
}
else
{
    Write-Host "Formatting file not found: $CustomConfigPath`n" -ForegroundColor Yellow
}

Write-Host "=============================================" -ForegroundColor Green
Write-Host "Starting dotnet format in $Mode mode" -ForegroundColor Green
Write-Host "=============================================`n" -ForegroundColor Green

if ($Mode -eq "check")
{
    dotnet format style $SlnPath --verify-no-changes --report $ReportOutputDir
}
else
{
    dotnet format style $SlnPath --severity warn --report $ReportOutputDir
}

Write-Host "`nOperation complete. Reports are located in: $ReportOutputDir`n" -ForegroundColor Green

if ($Visualize -and (Test-Path $ReportOutputDir))
{
    $JsonFile = Join-Path $ReportOutputDir "format-report.json"
    $HtmlFile = Join-Path $ReportOutputDir "format-report.html"

    if (Test-Path $JsonFile)
    {
        Write-Host "=============================================" -ForegroundColor Green
        Write-Host "Generating HTML report..." -ForegroundColor Green
        Write-Host "=============================================`n" -ForegroundColor Green

        $reportData = Get-Content $JsonFile -Raw | ConvertFrom-Json

        $htmlContent = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Dotnet Format Report</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; padding: 20px; background-color: #f4f6f8; color: #333; }
        h1 { color: #005A9E; text-align: center; margin-bottom: 2rem; }
        .file-card { background: white; margin-bottom: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }
        .file-header { background-color: #0078D7; color: white; padding: 12px 20px; font-weight: bold; word-break: break-all; }
        .file-path { font-size: 0.8em; font-weight: normal; opacity: 0.9; margin-top: 4px; }
        table { width: 100%; border-collapse: collapse; }
        th, td { padding: 12px 20px; text-align: left; border-bottom: 1px solid #eee; }
        th { background-color: #f8f9fa; font-weight: 600; color: #555; }
        tr:last-child td { border-bottom: none; }
        tr:hover { background-color: #f1f5f9; }
        .diagnostic { color: #d63384; font-family: monospace; background: #fdf5f8; padding: 2px 6px; border-radius: 4px; }
        .location { color: #055160; font-family: monospace; font-weight: bold; }
    </style>
</head>
<body>
    <h1>Dotnet Format - $Mode Report</h1>
"@
        if ($null -eq $reportData -or $reportData.Length -eq 0)
        {
            $htmlContent += "<div style='text-align:center; padding:40px; background:white; border-radius:8px;'><h3>No formatting issues found! Code is perfectly styled.</h3></div>"
        }
        else
        {
            foreach ($file in $reportData)
            {
                $htmlContent += @"
    <div class="file-card">
        <div class="file-header">
            $($file.FileName)
            <div class="file-path">$($file.FilePath)</div>
        </div>
        <table>
            <thead>
                <tr>
                    <th width="15%">Location</th>
                    <th width="20%">Diagnostic ID</th>
                    <th width="65%">Description</th>
                </tr>
            </thead>
            <tbody>
"@
                foreach ($change in $file.FileChanges)
                {
                    $htmlContent += @"
                <tr>
                    <td class="location">L$($change.LineNumber):C$($change.CharNumber)</td>
                    <td><span class="diagnostic">$($change.DiagnosticId)</span></td>
                    <td>$([System.Security.SecurityElement]::Escape($change.FormatDescription))</td>
                </tr>
"@
                }
                
                $htmlContent += @"
            </tbody>
        </table>
    </div>
"@
            }
        }

        $htmlContent += @"
</body>
</html>
"@
        Set-Content -Path $HtmlFile -Value $htmlContent -Encoding UTF8
        
        Write-Host "Opening report in browser...`n" -ForegroundColor Cyan
        Start-Process $HtmlFile
    }
}
