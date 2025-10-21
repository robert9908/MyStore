# OrderService - Production-Ready Microservice

## Overview

The OrderService is a production-grade microservice built with .NET 8 that handles order management for the MyStore e-commerce platform. It provides comprehensive order lifecycle management with enterprise-level security, monitoring, and scalability features.

## Features

### Core Functionality
- **Order Management**: Create, retrieve, update, and cancel orders
- **Order Status Tracking**: Complete order lifecycle from pending to delivered
- **Order Items Management**: Support for multiple items per order with discounts
- **User Order History**: Paginated order history for customers
- **Admin Order Management**: Administrative oversight of all orders

### Security Features
- **JWT Authentication**: Secure token-based authentication
- **Role-Based Authorization**: Customer and Admin role separation
- **Order Ownership Validation**: Users can only access their own orders
- **Input Validation**: Comprehensive validation using FluentValidation
- **Security Headers**: CORS, HTTPS enforcement, and security headers

### Production Features
- **Structured Logging**: Serilog with multiple sinks (Console, File, Database)
- **Health Checks**: Database connectivity and service health monitoring
- **Metrics & Monitoring**: Prometheus metrics integration
- **Caching**: Redis-based caching for performance optimization
- **Message Queue**: RabbitMQ integration for event-driven architecture
- **API Documentation**: Swagger/OpenAPI with security definitions
- **Retry Policies**: Polly for resilient HTTP communications
- **Graceful Shutdown**: Proper application lifecycle management

### Architecture
- **Clean Architecture**: Separation of concerns with proper layering
- **Repository Pattern**: Data access abstraction
- **Dependency Injection**: Comprehensive DI container setup
- **AutoMapper**: Object-to-object mapping
- **Middleware Pipeline**: Custom middleware for cross-cutting concerns
- **Configuration Management**: Environment-specific configurations

## Technology Stack

- **.NET 8**: Latest LTS version with performance improvements
- **Entity Framework Core 8**: Modern ORM with PostgreSQL provider
- **PostgreSQL**: Robust relational database
- **Redis**: High-performance caching and session storage
- **RabbitMQ**: Message broker for asynchronous communication
- **Serilog**: Structured logging framework
- **FluentValidation**: Powerful validation library
- **AutoMapper**: Object mapping library
- **Prometheus**: Metrics collection and monitoring
- **Polly**: Resilience and transient-fault-handling library

## Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL 13+
- Redis 6+
- RabbitMQ 3.8+
- Docker (optional)

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd OrderService
   ```

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure database**
   - Update connection string in `appsettings.Development.json`
   - Run database migrations:
   ```bash
   dotnet ef database update
   ```

4. **Start dependencies**
   ```bash
   # Start PostgreSQL, Redis, and RabbitMQ
   docker-compose up -d postgres redis rabbitmq
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the application**
   - API: `https://localhost:7001`
   - Swagger UI: `https://localhost:7001/swagger`
   - Health Checks: `https://localhost:7001/health`
   - Health UI: `https://localhost:7001/health-ui`

### Docker Deployment

1. **Build Docker image**
   ```bash
   docker build -t orderservice:latest .
   ```

2. **Run with Docker Compose**
   ```bash
   docker-compose up -d
   ```

## Configuration

### Environment Variables (Production)

```bash
# Database
DB_HOST=your-postgres-host
DB_NAME=MyStore_Orders
DB_USER=your-db-user
DB_PASSWORD=your-db-password

# Redis
REDIS_CONNECTION_STRING=your-redis-connection

# JWT
JWT_SECRET_KEY=your-super-secret-jwt-key-minimum-32-characters
JWT_ISSUER=MyStore.OrderService
JWT_AUDIENCE=MyStore.Client

# RabbitMQ
RABBITMQ_HOST=your-rabbitmq-host
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=your-rabbitmq-user
RABBITMQ_PASSWORD=your-rabbitmq-password
RABBITMQ_VHOST=/

# Frontend URLs
FRONTEND_URL=https://your-frontend-domain.com
ADMIN_PANEL_URL=https://your-admin-domain.com
```

### Configuration Files

- `appsettings.json`: Base configuration
- `appsettings.Development.json`: Development overrides
- `appsettings.Production.json`: Production configuration with environment variables

## API Endpoints

### Orders

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/orders` | Create new order | Customer/Admin |
| GET | `/api/orders/{id}` | Get order by ID | Owner/Admin |
| GET | `/api/orders/my-orders` | Get user's orders | Customer |
| GET | `/api/orders` | Get all orders (paginated) | Admin |
| PUT | `/api/orders/{id}/status` | Update order status | Admin |
| POST | `/api/orders/{id}/cancel` | Cancel order | Owner/Admin |

### Health & Monitoring

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Health check endpoint |
| GET | `/health-ui` | Health check dashboard |
| GET | `/metrics` | Prometheus metrics |
| GET | `/swagger` | API documentation |

## Database Schema

### Orders Table
- `Id` (UUID, Primary Key)
- `UserId` (String, Required)
- `Status` (Enum: Pending, Confirmed, Processing, Shipped, Delivered, Cancelled)
- `TotalAmount` (Decimal)
- `ShippingCost` (Decimal)
- `TaxAmount` (Decimal)
- `ShippingAddress` (String)
- `PaymentMethod` (String)
- `PaymentTransactionId` (String, Optional)
- `Notes` (String, Optional)
- `CreatedAt` (DateTime)
- `ConfirmedAt` (DateTime, Optional)
- `ShippedAt` (DateTime, Optional)
- `DeliveredAt` (DateTime, Optional)
- `CancelledAt` (DateTime, Optional)

### OrderItems Table
- `Id` (UUID, Primary Key)
- `OrderId` (UUID, Foreign Key)
- `ProductId` (String, Required)
- `ProductName` (String, Required)
- `ProductDescription` (String, Optional)
- `ProductImageUrl` (String, Optional)
- `ProductSku` (String, Optional)
- `Quantity` (Integer, Required)
- `Price` (Decimal, Required)
- `DiscountAmount` (Decimal, Default: 0)

## Security

### Authentication
- JWT Bearer tokens with configurable expiry
- Refresh token support (configured in AuthService)
- Token validation with issuer and audience verification

### Authorization
- Role-based access control (Customer, Admin)
- Resource-based authorization (users can only access their own orders)
- Order modification restrictions based on status

### Input Validation
- Comprehensive validation rules using FluentValidation
- Business rule validation (order totals, status transitions)
- SQL injection prevention through parameterized queries

## Monitoring & Observability

### Logging
- Structured logging with Serilog
- Multiple log sinks: Console, File, Database
- Configurable log levels per environment
- Request/response logging middleware

### Health Checks
- Database connectivity checks
- Redis connectivity checks
- Custom business logic health checks
- Health check UI dashboard

### Metrics
- Prometheus metrics collection
- Custom business metrics
- Performance counters
- Request/response metrics

## Performance & Scalability

### Caching Strategy
- Redis-based distributed caching
- Order data caching with TTL
- Cache invalidation on data changes
- Configurable cache expiration policies

### Database Optimization
- Proper indexing on frequently queried columns
- Pagination for large result sets
- Connection pooling
- Query optimization

### Async Operations
- Async/await pattern throughout
- Non-blocking I/O operations
- Background task processing
- Message queue integration

## Testing

### Unit Tests
```bash
dotnet test OrderService.Tests
```

### Integration Tests
```bash
dotnet test OrderService.IntegrationTests
```

### Load Testing
- Use tools like k6 or Artillery
- Test endpoints under various loads
- Monitor performance metrics

## Deployment

### Docker
```bash
# Build
docker build -t orderservice:v1.0.0 .

# Run
docker run -d -p 8080:8080 --name orderservice orderservice:v1.0.0
```

### Kubernetes
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: orderservice
spec:
  replicas: 3
  selector:
    matchLabels:
      app: orderservice
  template:
    metadata:
      labels:
        app: orderservice
    spec:
      containers:
      - name: orderservice
        image: orderservice:v1.0.0
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
```

## Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Verify connection string
   - Check database server availability
   - Ensure proper credentials

2. **Redis Connection Issues**
   - Verify Redis server is running
   - Check Redis connection string
   - Validate Redis credentials

3. **JWT Token Issues**
   - Verify JWT secret key configuration
   - Check token expiry settings
   - Validate issuer and audience

### Logs Location
- Development: Console and `logs/` directory
- Production: Configured log sinks (file, database)

### Health Check Endpoints
- `/health` - Basic health status
- `/health-ui` - Detailed health dashboard

## Contributing

1. Follow the existing code style and patterns
2. Add unit tests for new functionality
3. Update documentation for API changes
4. Ensure all health checks pass
5. Test in development environment before deployment

## License

This project is part of the MyStore e-commerce platform.

## Support

For technical support or questions:
- Check the health endpoints for service status
- Review logs for error details
- Consult the API documentation at `/swagger`
