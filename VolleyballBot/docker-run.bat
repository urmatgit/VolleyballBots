# Build Docker image
docker build -t volleyball-bot .

# Run with docker-compose
docker-compose up -d

# View logs
docker-compose logs -f

# Stop
docker-compose down

# Restart
docker-compose restart

# Update and restart
docker-compose pull && docker-compose up -d
