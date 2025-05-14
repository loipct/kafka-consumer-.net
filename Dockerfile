# -------------------------------
# Build stage
# -------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copy and restore dependencies
COPY src/*.csproj ./
RUN dotnet restore

# Copy all source and publish
COPY src/ ./
RUN dotnet publish -c Release -o /app/out

# -------------------------------
# Runtime stage
# -------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Install Kafka native dependencies (for Confluent.Kafka)
RUN apt-get update && \
    apt-get install -y librdkafka-dev ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# Copy published app from build stage
COPY --from=build /app/out ./

# Set environment variables for production and Kafka compatibility
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0

# Run as non-root user (recommended for Docker/K8s security)
RUN useradd -m appuser
USER appuser

# Define entry point
ENTRYPOINT ["dotnet", "kafka-consumer.dll"]
