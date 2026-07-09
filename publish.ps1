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
    $nupkgs = Get-ChildItem "src\$package\bin\Release\$package.*.nupkg" -ErrorAction SilentlyContinue |
              Where-Object { $_.Name -notlike "*.symbols.nupkg" }

    if ($nupkgs.Count -eq 0) {
        Write-Host "  Package NOT FOUND in src\$package\bin\Release\" -ForegroundColor Red
        continue
    }

    $path = ($nupkgs | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName

    Write-Host "  Publishing $package from $path..." -ForegroundColor Green
    dotnet nuget push $path --api-key $ApiKey --source $source --skip-duplicate

    if ($LASTEXITCODE -eq 0) {
        Write-Host "    $package published!" -ForegroundColor Green
    } else {
        Write-Host "    $package failed or already exists" -ForegroundColor Yellow
    }
}

Write-Host "Publishing complete!" -ForegroundColor Cyan