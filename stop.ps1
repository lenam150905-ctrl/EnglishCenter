# English Center - Stop Script
Write-Host \"Stopping English Center services...\" -ForegroundColor Yellow
docker compose down
Write-Host \"Services stopped.\" -ForegroundColor Green
