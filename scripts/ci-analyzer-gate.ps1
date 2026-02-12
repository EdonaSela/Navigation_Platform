param(
    [Parameter(Mandatory = $true)]
    [string]$BuildLogPath,

    [string]$BaselinePath = ".ci/analyzer-baseline.txt",

    [switch]$UpdateBaseline
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path $BuildLogPath)) {
    throw "Build log not found: $BuildLogPath"
}

function To-RepoRelativeText {
    param([string]$Text)

    $value = $Text.Replace('\', '/')

    $anchors = @(
        "src/",
        "tests/",
        "frontend/",
        "ops/",
        "scripts/",
        ".github/",
        "Directory.Build.props",
        "NavigationPlatform.sln",
        "docker-compose.yml"
    )

    foreach ($anchor in $anchors) {
        $idx = $value.IndexOf($anchor, [System.StringComparison]::OrdinalIgnoreCase)
        if ($idx -ge 0) {
            return $value.Substring($idx)
        }
    }

    return $value
}

function Normalize-WarningLine {
    param([string]$Line)

    $trimmed = $Line.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        return $null
    }

    # Example: path/file.cs(12,34): warning SA1101: ... [project.csproj]
    $pattern = '^(?<file>.+?)\((?<line>\d+),(?<col>\d+)\): warning (?<code>[A-Z]{2}\d{4}): (?<message>.+?) \[(?<project>.+?)\]$'
    $match = [regex]::Match($trimmed, $pattern)
    if ($match.Success) {
        $file = To-RepoRelativeText -Text $match.Groups["file"].Value
        $code = $match.Groups["code"].Value
        $message = $match.Groups["message"].Value.Trim()
        return "$code|$file|$message"
    }

    # Fallback for non-file warnings.
    if ($trimmed -match ': warning [A-Z]{2}\d{4}:') {
        return To-RepoRelativeText -Text $trimmed
    }

    return $null
}

$warnings = New-Object System.Collections.Generic.HashSet[string]
Get-Content $BuildLogPath | ForEach-Object {
    $normalized = Normalize-WarningLine -Line $_
    if ($null -ne $normalized) {
        $warnings.Add($normalized) | Out-Null
    }
}

$warningList = @($warnings) | Sort-Object

if ($UpdateBaseline) {
    $baselineDir = Split-Path -Parent $BaselinePath
    if (-not [string]::IsNullOrWhiteSpace($baselineDir) -and -not (Test-Path $baselineDir)) {
        New-Item -ItemType Directory -Path $baselineDir | Out-Null
    }

    $warningList | Set-Content -Path $BaselinePath -Encoding UTF8
    Write-Host "Analyzer baseline updated at $BaselinePath with $($warningList.Count) warnings."
    exit 0
}

if (-not (Test-Path $BaselinePath)) {
    throw "Baseline file not found: $BaselinePath. Run script with -UpdateBaseline to generate it."
}

$baseline = New-Object System.Collections.Generic.HashSet[string]
Get-Content $BaselinePath | ForEach-Object {
    $line = $_.Trim()
    if (-not [string]::IsNullOrWhiteSpace($line)) {
        $baseline.Add($line) | Out-Null
    }
}

$newWarnings = @()
foreach ($warning in $warningList) {
    if (-not $baseline.Contains($warning)) {
        $newWarnings += $warning
    }
}

if ($newWarnings.Count -gt 0) {
    Write-Host "New analyzer warnings detected: $($newWarnings.Count)" -ForegroundColor Red
    $newWarnings | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "Analyzer gate passed. No new warnings compared to baseline ($BaselinePath)." -ForegroundColor Green
