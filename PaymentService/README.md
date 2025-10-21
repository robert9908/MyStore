# PaymentService - Микросервис платежей

Продакшн-готовый микросервис обработки платежей для MyStore с полным набором функций безопасности и мониторинга.

## 🚀 Функции

### Основные возможности
- **Обработка платежей** с поддержкой множественных платежных шлюзов
- **Система возвратов** с частичными и полными возвратами
- **JWT аутентификация** с ролевой авторизацией
- **Валидация данных** с FluentValidation
- **Кеширование** с Redis для оптимизации производительности
- **Асинхронная обработка** всех операций
- **Webhook обработка** для уведомлений от платежных шлюзов

### Безопасность и мониторинг
- **Rate limiting** для защиты от злоупотреблений
- **Structured logging** с Serilog и correlation ID
- **Health checks** для мониторинга состояния сервиса
- **Prometheus метрики** для мониторинга производительности
- **Global exception handling** с детальными ошибками
- **CORS настройки** для безопасного взаимодействия с фронтендом

### Архитектура
- **Clean Architecture** с разделением слоев
- **Repository Pattern** для доступа к данным
- **SOLID принципы** в дизайне классов
- **Dependency Injection** для слабой связанности
- **AutoMapper** для маппинга между слоями

## 🛠 Технологии

- **.NET 8** - Основной фреймворк
- **PostgreSQL** - База данных
- **Redis** - Кеширование
- **Entity Framework Core** - ORM с Fluent API
- **FluentValidation** - Валидация данных
- **Serilog** - Структурированное логирование
- **AutoMapper** - Маппинг объектов
- **MassTransit + RabbitMQ** - Обмен сообщениями
- **Prometheus** - Метрики
- **Polly** - Resilience patterns
- **Swagger/OpenAPI** - Документация API

## 📋 Требования

- .NET 8 SDK
- PostgreSQL 13+
- Redis 6+
- RabbitMQ 3.8+
- Платежный шлюз (Stripe, PayPal и др.)

## ⚙️ Настройка

### 1. Клонирование и установка зависимостей

```bash
cd PaymentService
dotnet restore
```

### 2. Настройка базы данных

Создайте базу данных PostgreSQL и обновите строку подключения в `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=PaymentServiceDb;Username=postgres;Password=your_password;"
  }
}
```

### 3. Настройка Redis

Установите и запустите Redis:

```bash
# Windows (через Chocolatey)
choco install redis-64

# Linux/Mac
sudo apt-get install redis-server
# или
brew install redis
```

### 4. Настройка RabbitMQ

Установите RabbitMQ и создайте пользователя:

```bash
# Установка (Ubuntu)
sudo apt-get install rabbitmq-server

# Создание пользователя
sudo rabbitmqctl add_user mystore_user password
sudo rabbitmqctl set_permissions -p / mystore_user ".*" ".*" ".*"
```

### 5. Настройка платежного шлюза

Обновите настройки платежного шлюза в `appsettings.json`:

```json
{
  "PaymentGateway": {
    "BaseUrl": "https://api.stripe.com/v1/",
    "PublicKey": "pk_test_your_stripe_public_key",
    "SecretKey": "sk_test_your_stripe_secret_key",
    "WebhookSecret": "whsec_your_webhook_secret"
  }
}
```

### 6. Миграции базы данных

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 🚀 Запуск

### Development режим

```bash
dotnet run --environment Development
```

### Production режим

```bash
dotnet run --environment Production
```

### Docker

```bash
# Сборка образа
docker build -t paymentservice .

# Запуск контейнера
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production paymentservice
```

## 📊 API Endpoints

### Платежи

- `POST /api/v1/payments` - Создание платежа
- `GET /api/v1/payments/{id}` - Получение платежа по ID
- `GET /api/v1/payments/user` - Получение платежей пользователя
- `GET /api/v1/payments` - Получение всех платежей (только админ)
- `PUT /api/v1/payments/{id}/status` - Обновление статуса платежа

### Возвраты

- `POST /api/v1/payments/{id}/refunds` - Создание возврата
- `GET /api/v1/refunds/{id}` - Получение возврата по ID
- `GET /api/v1/payments/{id}/refunds` - Получение возвратов по платежу

### Webhooks

- `POST /api/v1/webhooks/payment-gateway` - Webhook от платежного шлюза

### Мониторинг

- `GET /health` - Health check
- `GET /health-ui` - Health check UI
- `GET /metrics` - Prometheus метрики

## 🏗 Архитектура

```
PaymentService/
├── Controllers/          # API контроллеры
├── Services/            # Бизнес-логика
├── Repositories/        # Доступ к данным
├── Entities/           # Модели данных
├── DTOs/               # Объекты передачи данных
├── Interfaces/         # Интерфейсы
├── Validators/         # Валидаторы FluentValidation
├── Middlewares/        # Кастомные middleware
├── Configurations/     # Конфигурационные классы
├── MappingProfiles/    # AutoMapper профили
├── HealthChecks/       # Health check классы
└── Data/               # DbContext и конфигурации EF
```

## 🔧 Конфигурация

### Переменные окружения (Production)

```bash
DATABASE_CONNECTION_STRING="Host=prod-db;Database=PaymentServiceDb;Username=app;Password=secure_password;"
REDIS_CONNECTION_STRING="prod-redis:6379"
RABBITMQ_CONNECTION_STRING="amqp://user:password@prod-rabbitmq:5672/"
JWT_SECRET_KEY="your-super-secure-jwt-secret-key-here"
JWT_ISSUER="MyStore.PaymentService"
JWT_AUDIENCE="MyStore.Client"
PAYMENT_GATEWAY_BASE_URL="https://api.stripe.com/v1/"
PAYMENT_GATEWAY_PUBLIC_KEY="pk_live_your_live_public_key"
PAYMENT_GATEWAY_SECRET_KEY="sk_live_your_live_secret_key"
PAYMENT_GATEWAY_WEBHOOK_SECRET="whsec_your_live_webhook_secret"
FRONTEND_URL="https://mystore.com"
ADMIN_PANEL_URL="https://admin.mystore.com"
```

## 📈 Мониторинг

### Health Checks

Сервис предоставляет несколько health check endpoints:

- `/health` - Основной health check
- `/health-ui` - Веб-интерфейс для мониторинга

### Метрики Prometheus

Доступны по адресу `/metrics`:

- HTTP запросы и их длительность
- Количество обработанных платежей
- Ошибки и исключения
- Использование памяти и CPU

### Логирование

Структурированное логирование с Serilog:

- Консольный вывод для разработки
- Файловое логирование с ротацией
- Correlation ID для трейсинга запросов
- Различные уровни логирования для разных сред

## 🔐 Безопасность

### Аутентификация и авторизация

- JWT Bearer токены
- Ролевая авторизация (User, Admin)
- Проверка владения ресурсами

### Rate Limiting

- Ограничения на создание платежей: 10 запросов в минуту
- Ограничения на возвраты: 5 запросов в 5 минут
- Настраиваемые лимиты через конфигурацию

### Валидация данных

- FluentValidation для всех входящих данных
- Проверка валютных кодов и методов оплаты
- Валидация сумм и описаний

## 🐛 Отладка и устранение неполадок

### Общие проблемы

1. **Ошибка подключения к базе данных**
   ```bash
   # Проверьте строку подключения
   # Убедитесь, что PostgreSQL запущен
   sudo systemctl status postgresql
   ```

2. **Ошибка подключения к Redis**
   ```bash
   # Проверьте статус Redis
   redis-cli ping
   ```

3. **Проблемы с RabbitMQ**
   ```bash
   # Проверьте статус RabbitMQ
   sudo systemctl status rabbitmq-server
   ```

### Логи

Логи сохраняются в папке `logs/`:

```bash
# Просмотр последних логов
tail -f logs/payment-service-*.log
```

### Health Checks

Проверьте состояние всех зависимостей:

```bash
curl http://localhost:8080/health
```

## 🚢 Развертывание

### Docker Compose

```yaml
version: '3.8'
services:
  paymentservice:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DATABASE_CONNECTION_STRING=Host=db;Database=PaymentServiceDb;Username=postgres;Password=postgres;
      - REDIS_CONNECTION_STRING=redis:6379
    depends_on:
      - db
      - redis
      - rabbitmq

  db:
    image: postgres:15
    environment:
      POSTGRES_DB: PaymentServiceDb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres

  redis:
    image: redis:7-alpine

  rabbitmq:
    image: rabbitmq:3-management
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
```

### Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: paymentservice
spec:
  replicas: 3
  selector:
    matchLabels:
      app: paymentservice
  template:
    metadata:
      labels:
        app: paymentservice
    spec:
      containers:
      - name: paymentservice
        image: paymentservice:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: DATABASE_CONNECTION_STRING
          valueFrom:
            secretKeyRef:
              name: paymentservice-secrets
              key: database-connection
```

## 🤝 Интеграция с другими сервисами

### OrderService

PaymentService интегрируется с OrderService для:

- Уведомления об успешных платежах
- Обновления статуса заказов
- Обработки отмененных заказов

### AuthService

Использует JWT токены от AuthService для:

- Аутентификации пользователей
- Проверки ролей и разрешений
- Валидации токенов

## 📝 Changelog

### v1.0.0
- Начальная версия с базовой функциональностью платежей
- Поддержка Stripe платежного шлюза
- Система возвратов
- Health checks и мониторинг

## 📄 Лицензия

MIT License - см. файл LICENSE для деталей.

## 👥 Команда разработки

- **Backend Team** - Основная разработка
- **DevOps Team** - Развертывание и мониторинг
- **QA Team** - Тестирование и качество

## 📞 Поддержка

Для получения поддержки:

1. Создайте issue в репозитории
2. Обратитесь к команде разработки
3. Проверьте документацию и FAQ

---

**PaymentService** - Надежный и масштабируемый сервис платежей для современных e-commerce решений.
