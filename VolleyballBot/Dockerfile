# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./ ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install dependencies
RUN apt-get update && apt-get install -y \
    tzdata \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Set timezone
ENV TZ=Europe/Moscow
ENV DOTNET_ENVIRONMENT=Production

WORKDIR /app

COPY --from=build /app/publish .

# Create data directory
RUN mkdir -p /data

# Volume for database
VOLUME ["/data"]

# Expose port for health check
EXPOSE 8080

# Set entrypoint
ENTRYPOINT ["dotnet", "VolleyballBot.dll"]
