# start.ps1

Write-Host "Запуск инфраструктуры (Jaeger и Prometheus)..." -ForegroundColor Green
docker-compose up -d

Write-Host "Запуск Service2..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd Service2; dotnet run"

Start-Sleep -Seconds 2

Write-Host "Запуск Service1..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd Service1; dotnet run"

Write-Host "Все сервисы запущены!" -ForegroundColor Yellow
Write-Host "Service2: http://localhost:5002" -ForegroundColor Cyan
Write-Host "Service1: http://localhost:5001" -ForegroundColor Cyan
Write-Host "Jaeger UI: http://localhost:16686" -ForegroundColor Cyan
Write-Host "Prometheus: http://localhost:9090" -ForegroundColor Cyan