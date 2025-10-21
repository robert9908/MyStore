# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["OrderService.csproj", "."]
RUN dotnet restore "OrderService.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "OrderService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN addgroup --system --gid 1001 orderservice && \
    adduser --system --uid 1001 --ingroup orderservice orderservice

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Set ownership and permissions
RUN chown -R orderservice:orderservice /app
USER orderservice

# Expose port
EXPOSE 8080
EXPOSE 8443

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080;https://+:8443
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "OrderService.dll"]
