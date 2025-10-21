# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY PaymentService.csproj .
RUN dotnet restore "PaymentService.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "PaymentService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "PaymentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN groupadd -r paymentservice && useradd -r -g paymentservice paymentservice

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory and set permissions
RUN mkdir -p /app/logs && chown -R paymentservice:paymentservice /app

# Switch to non-root user
USER paymentservice

# Expose port
EXPOSE 8080

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "PaymentService.dll"]
