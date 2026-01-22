# publish.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey
)

Write-Host "Cleaning..." -ForegroundColor Yellow
dotnet clean -c Release

Write-Host "Building and packing..." -ForegroundColor Yellow
dotnet pack -c Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Publishing to NuGet.org..." -ForegroundColor Cyan

$packages = @(
    "Brine2D",
    "Brine2D.SDL",
    "Brine2D.Tilemap",
    "Brine2D.UI"
)

$source = "https://api.nuget.org/v3/index.json"

foreach ($package in $packages) {
    $path = "src\$package\bin\Release\$package.0.9.0-beta.nupkg"
    
    if (Test-Path $path) {
        Write-Host "  Publishing $package..." -ForegroundColor Green
        dotnet nuget push $path --api-key $ApiKey --source $source --skip-duplicate
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "    $package published!" -ForegroundColor Green
        } else {
            Write-Host "    $package failed or already exists" -ForegroundColor Yellow
        }
    } else {
        Write-Host "    Package not found: $path" -ForegroundColor Red
    }
}

Write-Host "Publishing complete!" -ForegroundColor Cyan