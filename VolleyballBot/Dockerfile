# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /VolleyballBot

# Copy csproj and restore dependencies
COPY VolleyballBot.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./ ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install timezone data
RUN apt-get update && apt-get install -y tzdata && rm -rf /var/lib/apt/lists/*

# Set timezone
ENV TZ=Europe/Moscow

COPY --from=build /app/publish .

# Create volume for database and config
VOLUME ["/app/data"]

# Copy default config if not exists
COPY --from=build /app/publish/appsettings.json ./appsettings.json

ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "VolleyballBot.dll"]
