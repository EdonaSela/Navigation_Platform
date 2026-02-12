# Testing and Coverage

## Back-end (.NET)

Run tests with coverage:

```powershell
dotnet test tests/JourneyService.Api.Tests/JourneyService.Api.Tests.csproj --settings tests/coverage.runsettings --collect:"XPlat Code Coverage"
```

Coverage collector targets controller code in `JourneyService.Api` and writes Cobertura XML under:

`tests/JourneyService.Api.Tests/TestResults/**/coverage.cobertura.xml`

## Front-end (Angular)

Run tests with coverage:

```powershell
cd frontend
npm run test:coverage -- --watch=false
```

Cobertura XML is expected under:

`frontend/coverage/**/cobertura*.xml`

## Generate badges and test report

Run end-to-end:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-tests-and-report.ps1
```

If front-end tests cannot run in your environment:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-tests-and-report.ps1 -SkipFrontend
```

Outputs:

- `reports/badges/backend-coverage.svg`
- `reports/badges/frontend-coverage.svg` (if frontend coverage exists)
- `reports/test-report.md`
