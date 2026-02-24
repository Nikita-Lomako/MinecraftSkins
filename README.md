# MinecraftSkins (BTC-indexed pricing)

Сервис продажи Minecraft-скинов, где финальная цена рассчитывается динамически на основе курса BTC/USD.

Проект выполнен в формате mini production приложения:
- разделенная архитектура (Domain / Application / Infrastructure / API + отдельный Frontend на React + Vite + Js),
- централизованные зависимости и конфигурация,
- аккуратная интеграция с внешними сервисами (курс BTC, Redis, Prometheus, Grafana),
- воспроизводимый запуск через Docker Compose.

## Технологии

### Backend
- .NET 8, ASP.NET Core Minimal API
- EF Core + PostgreSQL
- ASP.NET Core Identity + JWT Bearer auth
- FluentValidation, AutoMapper
- IMemoryCache + Redis (L1/L2 cache для курса BTC)
- HttpClientFactory + resilience handlers (timeout/retry/circuit breaker/rate limiting)
- Serilog
- OpenTelemetry + Prometheus exporter + Grafana

### Frontend
- React + Vite + JavaScript
- React Router
- разделение по зонам ответственности(FSD) (`app/pages/features/entities/shared`)

## Содержание

- Архитектура
- Бизнес-правила
- API
- Обработка ошибок
- Конфигурация
- Запуск
- Миграции и сидирование
- Observability (OpenTelemetry/Prometheus/Grafana)
- Тесты

## Архитектура

Решение разделено на 4 backend-проекта + отдельный frontend:

- `MinecraftSkins.Domain`
  - сущности и доменные контракты без инфраструктурных зависимостей;
  - `Skin`, `Purchase`, `BtcRateResult`;
  - интерфейсы репозиториев и провайдеров (`ISkinRepository`, `IPurchaseRepository`, `IBtcRateProvider`).

- `MinecraftSkins.Application`
  - use-cases и бизнес-правила;
  - сервисы: `SkinService`, `PurchaseService`, `BtcRateService`, `AuthService`;
  - `IPriceCalculator` + стратегии `StandardPriceCalculator`, `PromoPriceCalculator`;
  - DTO, маппинг, валидация.

- `MinecraftSkins.Infrastructure`
  - EF Core (`AppDbContext`), миграции, репозитории;
  - интеграции с внешними API курса BTC (CoinGecko/Binance);
  - persistence, query-фильтры, soft delete, concurrency handling.

- `MinecraftSkins.Api`
  - composition root, DI, middleware;
  - Minimal API endpoints, Swagger;
  - JWT auth, ProblemDetails, health checks;
  - idempotency filter;
  - OpenTelemetry metrics pipeline + `/metrics`.

- `minecraftskins.front`
  - SPA, интеграция с backend API;
  - регистрация/аутентификация, каталог скинов, детали скина, список покупок, admin-страницы(просмотр курса, удаление/обновление/добавление скинов).

Ключевой принцип: API зависит от Application/Infrastructure, Application и Infrastructure зависят от Domain,Domain-слой не от кого не зависит.

## Бизнес-правила

### 1) Курс BTC/USD и отказоустойчивость

`BtcRateService` реализует 3-уровневую стратегию:
- L1: `IMemoryCache` (быстрый cache);
- L2: `IDistributedCache` (Redis);
- внешний провайдер через `IBtcRateProvider`.

Если внешний API недоступен:
- используется fallback на последнее успешное значение (если не слишком старое);
- если fallback устарел/отсутствует -> `503 Service Unavailable`.

### 2) Расчет финальной цены

Цена вынесена в отдельный компонент `IPriceCalculator`, чтобы формулу можно было тестировать и заменять без изменений API-слоя.

**Используемая формула (Standard):**

1. **Коэффициент изменения курса BTC:**  
   `btcGrowthFactor = btcPriceAtPurchase / btcPriceAtRelease`  
   (`btcPriceAtRelease` задаётся в конфиге, по умолчанию 68 000 USD).

2. **Ограничение волатильности (clamping):**  
   `clampedFactor = Math.Max(MinPriceMultiplier, Math.Min(MaxPriceMultiplier, btcGrowthFactor))`
   где MinPriceMultiplier = 0.5m и MaxPriceMultiplier = 3.0m
   — цена не опускается ниже 50% и не поднимается выше 300% от базы.

3. **Базовая цена и комиссия:**  
   `rawPrice = basePriceUsd * clampedFactor * (1 + liquidityFee)`  
   (`liquidityFee` по умолчанию 2%, задаётся в `PriceCalculator:LiquidityFee`).

4. **Округление:**  
   до 2 знаков после запятой (центы), `MidpointRounding.AwayFromZero`.

**Итог:**  
`finalPriceUsd = Round(basePriceUsd * clampedFactor * (1 + liquidityFee), 2)`.

Формула детерминирована, устойчива к крайним значениям (лимиты 0.5–3.0, проверка на нулевой курс). Стратегия расчёта (Standard/Promo) переключается конфигом и DI (в коде закомментирован пример выбора Promo).

### 3) Покупка

При покупке:
- проверяется существование и доступность скина;
- запрашивается курс BTC/USD;
- рассчитывается финальная цена;
- создается `Purchase`, где фиксируются `PriceUsdFinal` и `BtcUsdRate`.

Дополнительно:
- защита от повторов через `Idempotency-Key` на `POST /api/purchases`;
- защита целостности через optimistic concurrency и уникальные индексы.

## API

Полный интерактивный контракт доступен в Swagger: `https://localhost:8081/swagger`.

### Skins

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/skins` | Каталог с фильтрами `availableOnly`, `search`, `skip/take` (или пагинация), с расчетной ценой |
| GET | `/api/skins/{id}` | Детали скина + финальная цена |
| POST | `/api/skins` | Создание скина (`Admin`) |
| PUT | `/api/skins/{id}` | Обновление скина (`Admin`) |
| DELETE | `/api/skins/{id}` | Удаление (soft delete, `Admin`) |

### Rates

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/rates/btc-usd` | Текущий курс: `rate`, `asOfUtc`, `source` (`Cache/External/Fallback`), `ageSeconds` |

### Purchases

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/purchases` | Покупка скина. Body: `{ "skinId": "guid" }`, заголовок `Idempotency-Key` |
| GET | `/api/purchases` | Список покупок с фильтрами (`mineOnly/skinId/from/to/skip/take`) |
| GET | `/api/purchases/{id}` | Детали покупки |

### Auth

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/register` | Регистрация |
| POST | `/api/login` | Логин, возврат JWT |

### Health

| Method | Endpoint | Description |
|---|---|---|
| GET | `/health` | Health checks: БД, Redis, внешний провайдер курса |

## Обработка ошибок

Используется RFC 7807 ProblemDetails (`GlobalExceptionHandler`):
- `400` -> валидация и некорректные входные данные;
- `404` -> ресурс не найден;
- `409` -> конфликт бизнес-операции (например повторная покупка/недоступный ресурс);
- `503` -> внешний курс недоступен и fallback невозможен;
- `500` -> непредвиденная ошибка.

## Конфигурация

Ключевые настройки backend:
- `ConnectionStrings:DefaultConnection`
- `Redis:Configuration`
- `ApiSettings:Secret`
- `BtcRateProvider:Provider` (`CoinGecko` / `Binance`)
- `PriceCalculator:*`
- `Cors:AllowedOrigins`
- `OpenTelemetry:Metrics:Enabled`

Все параметры можно задать через:
- `MinecraftSkins.Api/appsettings.json`,
- environment variables (для Docker/CI).

## Запуск

### Docker Compose

В корне репозитория создайте файл **`.env`** (можно скопировать из примера ниже), задайте пароли и секреты.


**Пример .env:**


```env
# Database Configuration
DB_HOST=localhost
DB_NAME=MinecraftSkinsDb
DB_USER=postgres
DB_PASSWORD=ваш_пароль


# Redis Configuration
REDIS_HOST=localhost
REDIS_PORT=6379
REDIS_PASSWORD=ваш пароль для redis


# JWT Secret
JWT_SECRET=ваш_длинный_секрет_для_подписи_токенов


как пример можно взять:
K4t5N6R7u8V9wXyZaBcDeFgHiJkLmNoPqRsTuVwXyZaBcDeFgHiJkLmNoPqRsT
или
5f3c9a1b2d4e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1


#Vite + React
VITE_API_BASE_URL=https://localhost:8081/api
```


**Запуск:**


```bash
docker-compose up --build
```

3) Доступные сервисы:

| Service | URL |
|---|---|
| Frontend | `http://localhost:3001` |
| API HTTP | `http://localhost:8080` |
| API HTTPS | `https://localhost:8081` |
| Swagger | `http://localhost:8080/swagger` |
| Prometheus | `http://localhost:9090` |
| Grafana | `http://localhost:3000` |
| PostgreSQL | `localhost:5432` |
| Redis | `localhost:6379` |

Grafana credentials by default:
- login: `admin`
- password: `admin`



## Миграции и сидирование

При первом запуске API применяет миграции и создаёт seed-данные: 20 скинов, роли (Admin, User), пользователей **TestUser** / **Admin** / **TestUser2** (пароли: `Password123!` / `Admin123!` / `TestUser123!`).


Для создания миграции локально в appsettings.json заполните поля
"ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=MinecraftSkinsDb;Username=postgres;Password=ваш пароль"
  },
  и
  "Redis": {
    "Configuration": "localhost:6379,password=ваш пароль для redis"
  },
  После применения миграции рекомендуется убрать эти данные из appsettings.json и оставить строки пустыми(из env подтянутся).


## Observability (OpenTelemetry/Prometheus/Grafana)

### Что включено

- OpenTelemetry metrics pipeline в `Program.cs`:
  - `AddAspNetCoreInstrumentation()`
  - `AddHttpClientInstrumentation()`
  - `AddRuntimeInstrumentation()`
  - `AddPrometheusExporter()`
- endpoint метрик: `/metrics`
- Prometheus scrape настроен на `api:8080/metrics`
- Grafana datasource (`grafana/datasource.yml`)

### Быстрая проверка

1) Открыть `http://localhost:9090/targets`, убедиться что `minecraftskins-api` в статусе `UP`.
2) Открыть `http://localhost:8080/metrics`, убедиться что метрики отдаются.
3) В Grafana (`http://localhost:3000`) выполнить в Explore запрос:

```promql
up
```

## Тесты

Тестовый проект: `MinecraftSkins.Tests`.

- 100 тест-методов (`[Fact]`/`[Theory]`);
- 105 исполняемых тест-кейсов;
- line coverage: 86.25%;
- branch coverage: 57.18%.

Покрыты:
- unit-тесты сервисов, валидаторов, API контрактов, idempotency, exception mapping;
- integration-тесты endpoint-ов и репозиториев (включая PostgreSQL через Testcontainers);
- архитектурные тесты на границы слоев.

Запуск тестов:

```bash
dotnet test (здесь путь до проекта)MinecraftSkins\MinecraftSkins.Tests\MinecraftSkins.Tests.csproj" --no-restore -v minimal
```

## Frontend кратко

`minecraftskins.front` реализует:
- каталог скинов;
- покупку с `Idempotency-Key`;
- страницу покупок;
- admin-страницы для курса и управления скинами;
- role-based guards (`RequireAuth`, `RequireAdmin`).