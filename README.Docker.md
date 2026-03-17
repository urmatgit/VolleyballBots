# Docker commands for Volleyball Bot

## Build image
docker build -t volleyball-bot .

## Run with docker-compose (recommended)
docker-compose up -d

## View logs
docker-compose logs -f volleyball-bot

## Stop container
docker-compose down

## Restart container
docker-compose restart

## Update and redeploy
docker-compose pull
docker-compose up -d

## Run without docker-compose
docker run -d \
  --name volleyball-bot \
  --restart unless-stopped \
  -e BotToken="YOUR_BOT_TOKEN" \
  -e AdminTelegramIds="123456789" \
  -v volleyball-data:/app/data \
  volleyball-bot

## Access container shell
docker exec -it volleyball-bot /bin/bash

## View container logs
docker logs -f volleyball-bot

## Stop and remove container
docker stop volleyball-bot
docker rm volleyball-bot
