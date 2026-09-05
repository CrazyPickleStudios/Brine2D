# publish.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey
)

Write-Host "Cleaning..." -ForegroundColor Yellow
dotnet clean -c Release

Write-Host "Restoring..." -ForegroundColor Yellow
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Restore failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Building and packing..." -ForegroundColor Yellow
dotnet pack -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build/Pack failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Publishing to NuGet.org..." -ForegroundColor Cyan

$packages = @(
    "Brine2D",
    "Brine2D.Build"
)

$source = "https://api.nuget.org/v3/index.json"

foreach ($package in $packages) {
    $csprojPath = "src\$package\$package.csproj"
    $version = ([xml](Get-Content $csprojPath)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

    if (-not $version) {
        Write-Host "  Could not determine version from $csprojPath" -ForegroundColor Red
        continue
    }

    $expectedNupkg = "src\$package\bin\Release\$package.$version.nupkg"

    if (-not (Test-Path $expectedNupkg)) {
        Write-Host "  Package NOT FOUND: $expectedNupkg" -ForegroundColor Red
        continue
    }

    Write-Host "  Publishing $package $version from $expectedNupkg..." -ForegroundColor Green
    dotnet nuget push $expectedNupkg --api-key $ApiKey --source $source --skip-duplicate

    if ($LASTEXITCODE -eq 0) {
        Write-Host "    $package published!" -ForegroundColor Green
    } else {
        Write-Host "    $package failed or already exists" -ForegroundColor Yellow
    }
}

Write-Host "Publishing complete!" -ForegroundColor Cyan