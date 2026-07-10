<#
.SYNOPSIS
    Parses the benchmark.jsonl log file and computes latency statistics
    for cold-start enumeration sessions.

.DESCRIPTION
    Reads %APPDATA%\Vimium\logs\benchmark.jsonl, filters to entries with
    `cacheHit: false`, and computes mean, median, P95, min, and max for
    `elapsedMs`. Results are printed to the console.

.PARAMETER LogPath
    Path to the benchmark.jsonl file. Defaults to the standard location.

.PARAMETER IncludeCacheHits
    If set, includes cache-hit entries in the analysis (default: cold starts only).

.EXAMPLE
    powershell -File scripts/parse-benchmark-log.ps1
    powershell -File scripts/parse-benchmark-log.ps1 -IncludeCacheHits
#>

param(
    [string]$LogPath = "$env:APPDATA\Vimium\logs\benchmark.jsonl",
    [switch]$IncludeCacheHits
)

if (-not (Test-Path $LogPath)) {
    Write-Error "Log file not found: $LogPath"
    exit 1
}

$entries = @()
Get-Content $LogPath | ForEach-Object {
    $line = $_.Trim()
    if ($line) {
        try {
            $obj = $line | ConvertFrom-Json
            $entries += $obj
        } catch {
            Write-Warning "Skipping invalid JSON line: $($line.Substring(0, [Math]::Min(80, $line.Length)))..."
        }
    }
}

if (-not $IncludeCacheHits) {
    $entries = $entries | Where-Object { $_.cacheHit -eq $false }
}

if ($entries.Count -eq 0) {
    Write-Host "No entries found (filtered to cacheHit=false). Use -IncludeCacheHits to see all entries."
    exit 0
}

$times = $entries | ForEach-Object { $_.elapsedMs } | Sort-Object

$mean = ($times | Measure-Object -Average).Average
$min = $times[0]
$max = $times[-1]

# Median
if ($times.Count % 2 -eq 0) {
    $mid = $times.Count / 2
    $median = ($times[$mid - 1] + $times[$mid]) / 2.0
} else {
    $median = $times[[Math]::Floor($times.Count / 2)]
}

# P95
$p95Index = [Math]::Ceiling($times.Count * 0.95) - 1
$p95Index = [Math]::Max(0, [Math]::Min($p95Index, $times.Count - 1))
$p95 = $times[$p95Index]

Write-Host ""
Write-Host "═════════════════════════════════════════"
Write-Host "  Benchmark Analysis"
Write-Host "═════════════════════════════════════════"
Write-Host "  Repetitions : $($times.Count)"
Write-Host "  Mean        : $([Math]::Round($mean, 1)) ms"
Write-Host "  Median      : $([Math]::Round($median, 1)) ms"
Write-Host "  P95         : $([Math]::Round($p95, 1)) ms"
Write-Host "  Min         : $min ms"
Write-Host "  Max         : $max ms"
Write-Host "═════════════════════════════════════════"
Write-Host ""

# P95 validation
if ($p95 -lt 750) {
    Write-Host "✓ PASS: P95 ($([Math]::Round($p95, 1)) ms) < 750 ms target" -ForegroundColor Green
} else {
    Write-Host "✗ FAIL: P95 ($([Math]::Round($p95, 1)) ms) >= 750 ms target" -ForegroundColor Red
}

# Summary histogram
Write-Host ""
Write-Host "Distribution:"
$buckets = @(
    @{Label="< 100 ms"; Min=0; Max=99},
    @{Label="100-249 ms"; Min=100; Max=249},
    @{Label="250-499 ms"; Min=250; Max=499},
    @{Label="500-749 ms"; Min=500; Max=749},
    @{Label="750-999 ms"; Min=750; Max=999},
    @{Label=">= 1000 ms"; Min=1000; Max=[Int32]::MaxValue}
)

foreach ($bucket in $buckets) {
    $count = ($times | Where-Object { $_ -ge $bucket.Min -and $_ -le $bucket.Max }).Count
    $bar = "".PadLeft($count, '#')
    Write-Host "  $($bucket.Label.PadRight(14)) $($count.ToString().PadLeft(3)) $bar"
}
