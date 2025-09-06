#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs PowerTradePosition tests and generates comprehensive reports

.DESCRIPTION
    This script runs all unit tests, generates code coverage reports, and creates
    detailed HTML test reports with performance metrics.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Debug.

.PARAMETER OutputPath
    Directory where test reports will be saved. Default is ./TestReports.

.PARAMETER GenerateHtml
    Whether to generate HTML reports. Default is true.

.PARAMETER OpenReports
    Whether to open the generated reports in the default browser. Default is false.

.EXAMPLE
    .\run-tests-with-reports.ps1
    .\run-tests-with-reports.ps1 -Configuration Release -OutputPath ./Reports
    .\run-tests-with-reports.ps1 -OpenReports
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [string]$OutputPath = "./TestReports",
    
    [bool]$GenerateHtml = $true,
    
    [bool]$OpenReports = $false
)

# Set error action preference
$ErrorActionPreference = "Stop"

Write-Host "PowerTradePosition Test Runner with Reports" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow
Write-Host "Generate HTML: $GenerateHtml" -ForegroundColor Yellow
Write-Host ""

# Ensure output directory exists
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Host "Created output directory: $OutputPath" -ForegroundColor Green
}

# Change to solution directory (src folder contains the .sln file)
$solutionPath = Join-Path $PSScriptRoot "src"
if (!(Test-Path $solutionPath)) {
    Write-Error "Source directory not found: $solutionPath"
    exit 1
}
Set-Location $solutionPath
Write-Host "Changed to solution directory: $solutionPath" -ForegroundColor Green

Write-Host "Building solution..." -ForegroundColor Cyan
dotnet build --configuration $Configuration --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Please fix build errors before running tests."
    exit 1
}

Write-Host "Build completed successfully." -ForegroundColor Green

# Run tests with coverage
Write-Host "Running tests with coverage..." -ForegroundColor Cyan
$testResultsPath = Join-Path $OutputPath "TestResults"

# Ensure test results directory exists
if (!(Test-Path $testResultsPath)) {
    New-Item -ItemType Directory -Path $testResultsPath -Force | Out-Null
    Write-Host "Created test results directory: $testResultsPath" -ForegroundColor Green
}

dotnet test --configuration $Configuration --collect:"XPlat Code Coverage" --results-directory $testResultsPath --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Warning "Some tests failed. Check the test results above."
}

# Find coverage file (coverlet generates it in GUID-named subdirectories)
Write-Host "Locating coverage files..." -ForegroundColor Cyan
$coverageFiles = Get-ChildItem -Path $testResultsPath -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue

if ($coverageFiles) {
    $coveragePath = $coverageFiles[0].FullName
    Write-Host "Found coverage file: $coveragePath" -ForegroundColor Green
} else {
    Write-Warning "No coverage files found. This may indicate a test configuration issue."
    $coveragePath = Join-Path $testResultsPath "coverage.cobertura.xml"  # Set default path for reporting
}

# Generate HTML coverage report if ReportGenerator is available
if ($GenerateHtml) {
    Write-Host "Generating HTML coverage report..." -ForegroundColor Cyan
    
    try {
        # Check if ReportGenerator is installed globally
        $reportGeneratorPath = Get-Command reportgenerator -ErrorAction SilentlyContinue
        
        if (!$reportGeneratorPath) {
            Write-Host "Installing ReportGenerator globally..." -ForegroundColor Yellow
            try {
                dotnet tool install --global dotnet-reportgenerator-globaltool
                if ($LASTEXITCODE -ne 0) {
                    Write-Warning "Failed to install ReportGenerator globally. Trying alternative installation..."
                    dotnet tool install --global ReportGenerator
                }
            }
            catch {
                Write-Warning "Failed to install ReportGenerator: $_"
                Write-Host "You can manually install ReportGenerator with: dotnet tool install --global dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
                return
            }
        }
        
        # Verify ReportGenerator is now available
        $reportGeneratorPath = Get-Command reportgenerator -ErrorAction SilentlyContinue
        if (!$reportGeneratorPath) {
            Write-Warning "ReportGenerator is not available after installation attempt."
            return
        }
        
        $htmlOutputPath = Join-Path $testResultsPath "html"
        Write-Host "Generating HTML report using ReportGenerator..." -ForegroundColor Cyan
        reportgenerator -reports:$coveragePath -targetdir:$htmlOutputPath -reporttypes:Html
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "HTML coverage report generated successfully." -ForegroundColor Green
            Write-Host "Report location: $htmlOutputPath" -ForegroundColor Green
            
            if ($OpenReports) {
                $indexHtmlPath = Join-Path $htmlOutputPath "index.html"
                if (Test-Path $indexHtmlPath) {
                    Start-Process $indexHtmlPath
                    Write-Host "Opened coverage report in browser." -ForegroundColor Green
                }
            }
        }
        else {
            Write-Warning "ReportGenerator failed to generate HTML report. Exit code: $LASTEXITCODE"
        }
    }
    catch {
        Write-Warning "Failed to generate HTML coverage report: $_"
        Write-Host "You can manually install ReportGenerator with: dotnet tool install --global dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
    }
}

# Generate test summary report
Write-Host "Generating test summary report..." -ForegroundColor Cyan
$summaryReportPath = Join-Path $OutputPath "TestSummary.txt"

$testOutput = dotnet test --configuration $Configuration --logger:"console;verbosity=normal" --no-build 2>&1

# Extract test results from output
$passedTests = ($testOutput | Select-String -Pattern "Passed:\s*(\d+)" | ForEach-Object { $_.Matches.Groups[1].Value }) | Select-Object -First 1
$failedTests = ($testOutput | Select-String -Pattern "Failed:\s*(\d+)" | ForEach-Object { $_.Matches.Groups[1].Value }) | Select-Object -First 1
$skippedTests = ($testOutput | Select-String -Pattern "Skipped:\s*(\d+)" | ForEach-Object { $_.Matches.Groups[1].Value }) | Select-Object -First 1

# Set default values if extraction failed
if (!$passedTests) { $passedTests = "0" }
if (!$failedTests) { $failedTests = "0" }
if (!$skippedTests) { $skippedTests = "0" }

# Create summary report
$summaryReport = @"
PowerTradePosition Test Summary Report
=====================================
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Configuration: $Configuration

Test Results:
- Passed: $passedTests
- Failed: $failedTests
- Skipped: $skippedTests

Coverage Report: $coveragePath
HTML Report: $testResultsPath/html/index.html

Full Test Output:
$testOutput
"@

$summaryReport | Out-File -FilePath $summaryReportPath -Encoding UTF8
Write-Host "Test summary report generated: $summaryReportPath" -ForegroundColor Green

# Display final summary
Write-Host ""
Write-Host "Test Execution Complete!" -ForegroundColor Green
Write-Host "=======================" -ForegroundColor Green
Write-Host "Total Tests: $([int]$passedTests + [int]$failedTests + [int]$skippedTests)" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
Write-Host "Skipped: $skippedTests" -ForegroundColor Yellow
Write-Host ""
Write-Host "Reports generated in: $OutputPath" -ForegroundColor Cyan
Write-Host "Coverage report: $coveragePath" -ForegroundColor Cyan

if ($GenerateHtml -and (Test-Path (Join-Path $testResultsPath "html/index.html"))) {
    Write-Host "HTML report: $testResultsPath/html/index.html" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "To view HTML coverage report, open: $testResultsPath/html/index.html" -ForegroundColor Yellow
