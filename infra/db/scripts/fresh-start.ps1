Write-Host "[1/4] Останавливаю старый контейнер и удаляю volume..."
docker compose down -v --remove-orphans

Write-Host "[2/4] Удаляю контейнер vqms_postgres, если он остался..."
docker rm -f vqms_postgres 2>$null

Write-Host "[3/4] Поднимаю PostgreSQL заново..."
docker compose up -d

Write-Host "[4/4] Показываю последние строки лога..."
docker logs vqms_postgres --tail 80
