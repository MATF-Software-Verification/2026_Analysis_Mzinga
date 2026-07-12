param
(
    [Parameter(Mandatory = $false)]
    [switch]$Visualize,

    [Parameter(Mandatory = $false)]
    [switch]$Rerun
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BaseDir = Split-Path -Parent $ScriptDir

$EnginePath = Join-Path $BaseDir "Mzinga\src\Mzinga.Engine\bin\Release\net8.0\MzingaEngine.exe"
$EngineProject = Join-Path $BaseDir "Mzinga\src\Mzinga.Engine\Mzinga.Engine.csproj"
$ResultsDir = Join-Path $ScriptDir "Results"
$TraceOutput = Join-Path $ResultsDir "Report.nettrace"

if (!(Test-Path $EnginePath))
{
    Write-Host "Engine executable not found at $EnginePath. Building in Release configuration...`n" -ForegroundColor Yellow
    dotnet build $EngineProject -c Release
}

if (!(Get-Command dotnet-trace -ErrorAction SilentlyContinue))
{
    Write-Host "Installing dotnet-trace tool globally...`n" -ForegroundColor Green
    dotnet tool install -g dotnet-trace --ignore-failed-sources 2>&1 | Out-Null
}

$NeedsRun = $true

if (Test-Path $TraceOutput)
{
    if ($Rerun)
    {
        Write-Host "Cleaning previous profiling results`n" -ForegroundColor Green
        Remove-Item -Path $TraceOutput -Force -ErrorAction SilentlyContinue
    }
    else
    {
        Write-Host "Profiling results already exist. Skipping profiling. Use -Rerun to force re-execution.`n" -ForegroundColor Cyan
        $NeedsRun = $false
    }
}

if ($NeedsRun)
{
    if (!(Test-Path $ResultsDir))
    {
        New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
    }

    Write-Host "=============================================" -ForegroundColor Green
    Write-Host "Starting Mzinga Engine" -ForegroundColor Green
    Write-Host "=============================================`n" -ForegroundColor Green

    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $EnginePath
    $processInfo.RedirectStandardInput = $true
    $processInfo.RedirectStandardOutput = $true
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true

    $engine = [System.Diagnostics.Process]::Start($processInfo)
    $in = $engine.StandardInput
    $out = $engine.StandardOutput

    function Consume-Output()
    {
        while ($line = $out.ReadLine())
        {
            if ($line -eq "ok")
            {
                break
            }
        }
    }

    function Get-BestMove($timeoutStr)
    {
        $in.WriteLine("bestmove time $timeoutStr")
        $foundMove = ""
        while ($line = $out.ReadLine())
        {
            if ($line -eq "ok")
            {
                break
            }
            if (-not [string]::IsNullOrWhiteSpace($line) -and $line -notmatch "info" -and $line -notmatch "newgame" -and $line -notmatch "bestmove" -and $line -notmatch "Base" -and $line -notmatch "ok")
            {
                $foundMove = ($line -split ';')[0]
            }
        }
        return $foundMove
    }

    try
    {
        while ($line = $out.ReadLine())
        {
            if ($line -eq "ok")
            {
                break
            }
        }

        $in.WriteLine("info")
        Consume-Output
        $in.WriteLine("newgame")
        Consume-Output

        Write-Host "Playing first 8 turns..." -ForegroundColor Cyan
        for ($i = 1; $i -le 8; $i++)
        {
            $move = Get-BestMove "00:01:00" 
            Write-Host " -> Move $i played: $move"
            $in.WriteLine("play $move")
            Consume-Output
        }

        Write-Host "`nAttaching dotnet-trace profiler to process ID: $($engine.Id)" -ForegroundColor Yellow
        $traceProcessArgs = "collect -p $($engine.Id) --format NetTrace -o `"$TraceOutput`""
        $traceProcess = Start-Process -FilePath "dotnet-trace" -ArgumentList $traceProcessArgs -PassThru -WindowStyle Hidden
        
        Start-Sleep -Seconds 3

        Write-Host "`nPlaying next 8 turns..." -ForegroundColor Cyan
        for ($i = 9; $i -le 16; $i++)
        {
            $move = Get-BestMove "00:01:00" 
            Write-Host " -> Move $i played: $move"
            $in.WriteLine("play $move")
            Consume-Output
        }

        Write-Host "`nShutting down Mzinga Engine..." -ForegroundColor Green
        $in.WriteLine("exit")
        $engine.WaitForExit()

        Write-Host "Profiling successfully generated: $TraceOutput`n" -ForegroundColor Green
    }
    finally
    {
        if (!$engine.HasExited)
        {
            $engine.Kill()
        }
    }
}

if ($Visualize)
{
    if (Test-Path $TraceOutput)
    {
        Write-Host "Opening profiling report ($TraceOutput) in Visual Studio...`n" -ForegroundColor Green
        Invoke-Item $TraceOutput
    }
    else
    {
        Write-Host "Profiling report not found at $TraceOutput`n" -ForegroundColor Red
    }
}
