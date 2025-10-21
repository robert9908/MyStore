# AuthService - Микросервис аутентификации

Продакшн-готовый микросервис аутентификации для MyStore с полным набором функций безопасности.

## 🚀 Функции

- **JWT аутентификация** с Access и Refresh токенами
- **Двухфакторная аутентификация (2FA)** через email
- **Безопасное хеширование паролей** с BCrypt
- **Rate limiting** для защиты от брутфорса
- **Blacklist токенов** с Redis
- **Email подтверждение** регистрации
- **Сброс пароля** через email
- **Административная панель** для управления пользователями
- **Health checks** для мониторинга
- **Structured logging** с Serilog
- **Метрики Prometheus** для мониторинга
- **Swagger документация** API

## 🛠 Технологии

- **.NET 8** - Основной фреймворк
- **PostgreSQL** - База данных
- **Redis** - Кеширование и blacklist токенов
- **Entity Framework Core** - ORM
- **BCrypt.Net** - Хеширование паролей
- **FluentValidation** - Валидация данных
- **Serilog** - Структурированное логирование
- **Prometheus** - Метрики
- **Swagger/OpenAPI** - Документация API

## 📋 Требования

- .NET 8 SDK
- PostgreSQL 13+
- Redis 6+
- SMTP сервер для отправки email

## ⚙️ Настройка

### 1. Клонирование и установка зависимостей

```bash
cd AuthService
dotnet restore
```

### 2. Настройка базы данных

Создайте базу данных PostgreSQL и обновите строку подключения в `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authservice;Username=your_user;Password=your_password"
  }
}
```

### 3. Создание миграций

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Настройка конфигурации

Обновите `appsettings.json` с вашими настройками:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authservice;Username=postgres;Password=password",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "Secret": "your-super-secret-jwt-key-at-least-32-characters-long",
    "Issuer": "MyStore.AuthService",
    "Audience": "MyStore.Client",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "EmailSettings": {
    "From": "noreply@mystore.com",
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  },
  "SecuritySettings": {
    "RequireEmailConfirmation": true,
    "RequireStrongPasswords": true,
    "MaxFailedLoginAttempts": 5,
    "LockoutDurationMinutes": 30
  },
  "RateLimitSettings": {
    "LoginAttempts": {
      "MaxAttempts": 5,
      "WindowMinutes": 15
    },
    "RegistrationAttempts": {
      "MaxAttempts": 3,
      "WindowMinutes": 60
    }
  }
}
```

### 5. Запуск приложения

```bash
dotnet run
```

Приложение будет доступно по адресу: `https://localhost:7001`

## 📚 API Документация

После запуска приложения, Swagger UI доступен по адресу:
`https://localhost:7001/swagger`

### Основные эндпоинты:

#### Аутентификация
- `POST /api/auth/register` - Регистрация пользователя
- `POST /api/auth/login` - Вход в систему
- `POST /api/auth/refresh` - Обновление токена
- `POST /api/auth/logout` - Выход из системы
- `POST /api/auth/forgot-password` - Запрос сброса пароля
- `POST /api/auth/reset-password` - Сброс пароля
- `POST /api/auth/confirm-email` - Подтверждение email
- `POST /api/auth/enable-2fa` - Включение 2FA
- `POST /api/auth/verify-2fa` - Подтверждение 2FA кода

#### Администрирование
- `GET /api/admin/users` - Список всех пользователей
- `PUT /api/admin/users/{id}/role` - Изменение роли пользователя
- `PUT /api/admin/users/{id}/ban` - Блокировка пользователя
- `PUT /api/admin/users/{id}/unban` - Разблокировка пользователя
- `DELETE /api/admin/users/{id}` - Удаление пользователя

#### Мониторинг
- `GET /health` - Health check
- `GET /health-ui` - Health check UI
- `GET /metrics` - Prometheus метрики

## 🔒 Безопасность

### Реализованные меры безопасности:

1. **Хеширование паролей** - BCrypt с work factor 12
2. **JWT токены** - Короткоживущие access токены (15 мин) + refresh токены (7 дней)
3. **Rate limiting** - Защита от брутфорса и DDoS
4. **Token blacklisting** - Возможность отзыва токенов
5. **Account lockout** - Блокировка после неудачных попыток входа
6. **Email подтверждение** - Обязательное подтверждение email
7. **Двухфакторная аутентификация** - Дополнительная защита
8. **CORS настройки** - Контроль доступа с фронтенда
9. **Security headers** - Защитные HTTP заголовки
10. **Input validation** - Валидация всех входных данных

### Требования к паролям:
- Минимум 8 символов
- Минимум 1 заглавная буква
- Минимум 1 строчная буква  
- Минимум 1 цифра
- Минимум 1 специальный символ

## 🐳 Docker

### Dockerfile уже настроен для продакшн деплоя:

```bash
docker build -t authservice .
docker run -p 8080:8080 authservice
```

### Docker Compose с зависимостями:

```yaml
version: '3.8'
services:
  authservice:
    build: .
    ports:
      - "8080:8080"
    depends_on:
      - postgres
      - redis
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=authservice;Username=postgres;Password=password
      - ConnectionStrings__Redis=redis:6379

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: authservice
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data

volumes:
  postgres_data:
  redis_data:
```

## 📊 Мониторинг

### Health Checks
- Database connectivity
- Redis connectivity  
- Service health status

### Metrics (Prometheus)
- Request count and duration
- Authentication success/failure rates
- Active user sessions
- Error rates by endpoint

### Logging (Serilog)
- Structured JSON logs
- Multiple sinks: Console, File, PostgreSQL
- Log levels: Debug, Info, Warning, Error, Fatal
- Request/Response logging
- Security events logging
