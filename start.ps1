# English Center - Quick Start Script
# Run this in PowerShell as Administrator

Write-Host \"========================================\" -ForegroundColor Cyan
Write-Host \"  English Center - Docker Startup\" -ForegroundColor Cyan
Write-Host \"========================================\" -ForegroundColor Cyan
Write-Host \"\"

# Check if Docker is running
if (-not (Get-Service docker -ErrorAction SilentlyContinue)) {
    Write-Host \"Docker Desktop is not running. Starting...\" -ForegroundColor Yellow
    Start-Process \"C:\Program Files\Docker\Docker\Docker Desktop.exe\"
    Write-Host \"Waiting for Docker to start...\" -ForegroundColor Yellow
    Start-Sleep -Seconds 30
}

# Check if .env exists
if (-not (Test-Path \".env\")) {
    Write-Host \"Creating .env from .env.example...\" -ForegroundColor Yellow
    Copy-Item \".env.example\" \".env\" -Force
    Write-Host \"Please edit .env with your configuration before continuing!\" -ForegroundColor Red
    Read-Host \"Press Enter after editing .env to continue...\"
}

Write-Host \"Building and starting all services...\" -ForegroundColor Green
docker compose up --build -d

Write-Host \"\"
Write-Host \"========================================\" -ForegroundColor Cyan
Write-Host \"  Services Started!\" -ForegroundColor Cyan
Write-Host \"========================================\" -ForegroundColor Cyan
Write-Host \"Frontend:     http://localhost:5500\" -ForegroundColor Green
Write-Host \"Gateway:      http://localhost:7300\" -ForegroundColor Green
Write-Host \"API (direct): http://localhost:7207\" -ForegroundColor Green
Write-Host \"Swagger:      http://localhost:7207/swagger\" -ForegroundColor Green
Write-Host \"SQL Server:   localhost,1433\" -ForegroundColor Green
Write-Host \"Redis:        localhost:6379\" -ForegroundColor Green
Write-Host \"\"
Write-Host \"To view logs: docker compose logs -f\" -ForegroundColor Yellow
Write-Host \"To stop:      docker compose down\" -ForegroundColor Yellow
Write-Host \"To stop + wipe data: docker compose down -v\" -ForegroundColor Yellow
