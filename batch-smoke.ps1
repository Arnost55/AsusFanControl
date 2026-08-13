param(
    [int]$TimeoutSeconds = 20,
    [string]$ExecutablePath = (Join-Path $PSScriptRoot 'out-layout\AsusFanControlNative.exe')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ExecutablePath)) {
    Write-Error "Missing executable: $ExecutablePath"
    exit 1
}

$process = Start-Process -FilePath $ExecutablePath -PassThru
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$reportedWindow = $false

while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 1

    $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    if (-not $running) {
        Write-Output "exited: pid=$($process.Id)"
        exit 0
    }

    $running.Refresh()
    if (-not $reportedWindow -and $running.MainWindowHandle -ne 0) {
        $reportedWindow = $true
        Write-Output "window: title='$($running.MainWindowTitle)' pid=$($process.Id)"
    }
}

$stillRunning = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
if ($stillRunning) {
    Write-Output "timeout: killing pid=$($process.Id)"
    Stop-Process -Id $process.Id -Force
}
