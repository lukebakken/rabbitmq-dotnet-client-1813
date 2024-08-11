::Create redis insight volume folder with respective user onwership
bash -exec "sudo sh -c 'mkdir -p /var/exchangeratemanager/valkey/insight_data; chown -R 1000:1000 /var/exchangeratemanager/valkey/insight_data/'"

docker compose -f .\docker-compose.yml -f .\docker-compose.local.yml up -d
::docker build -f .\ExchangeRateManager\Dockerfile --force-rm -t ExchangeRateManager/latest .
::docker run -d --network exchangeratemanager_network --name ExchangeRateManager ExchangeRateManager/latest