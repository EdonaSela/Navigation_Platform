param(
  [switch]$SkipFrontend
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "Running backend tests with coverage..."
dotnet test "$repoRoot\tests\JourneyService.Api.Tests\JourneyService.Api.Tests.csproj" `
  --settings "$repoRoot\tests\coverage.runsettings" `
  --collect:"XPlat Code Coverage"

$backendCoverage = Get-ChildItem "$repoRoot\tests\JourneyService.Api.Tests\TestResults" -Recurse -Filter "coverage.cobertura.xml" |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 1

if (-not $backendCoverage) {
  throw "Backend coverage file was not generated."
}

$frontendCoverageArg = ""
if (-not $SkipFrontend) {
  Write-Host "Running frontend tests with coverage..."
  Push-Location "$repoRoot\frontend"
  try {
    npm test -- --watch=false --code-coverage
  } finally {
    Pop-Location
  }

  $frontendCoverage = Get-ChildItem "$repoRoot\frontend\coverage" -Recurse -Filter "cobertura*.xml" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

  if ($frontendCoverage) {
    $frontendCoverageArg = $frontendCoverage.FullName
  }
}

Write-Host "Generating badges and markdown report..."
$scriptPath = "$repoRoot\scripts\generate_test_report.py"
$args = @(
  $scriptPath,
  "--backend-cobertura", $backendCoverage.FullName,
  "--output-dir", "$repoRoot\reports"
)

if ($frontendCoverageArg) {
  $args += @("--frontend-cobertura", $frontendCoverageArg)
}

python @args

Write-Host "Done. Report: $repoRoot\reports\test-report.md"
