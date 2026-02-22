# MinecraftSkins

Сервис продажи Minecraft-скинов с привязкой цены к курсу BTC/USD. 
В ТЗ: разделённая архитектура, чёткие границы слоёв, управляемые зависимости, интеграция с внешними API.

**Backend:** ASP.NET Core 8, Minimal API, EF Core.  
**Frontend:** React + Vite + JavaScript (отдельный проект в minecraftskins.front).

---

## Содержание

- Запуск
- Миграции
- Формула расчёта цены
- API (endpoints)
- Архитектура
- Конфигурация

---

## Запуск

### Через Docker Compose (рекомендуется)

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

После старта:

| Сервис      | URL |
|------------|-----|
| **Фронтенд** | http://localhost:3000 |
| **API (HTTP)** | http://localhost:8080 |
| **Swagger** | http://localhost:8080/swagger |
| **API (HTTPS)** | https://localhost:8081 |
| **PostgreSQL** | localhost:5432 |
| **Redis** | localhost:6379 |

При первом запуске API применяет миграции и создаёт seed-данные: 20 скинов, роли (Admin, User), пользователей **TestUser** / **Admin** / **TestUser2** (пароли: `Password123!` / `Admin123!` / `TestUser123!`).

Для создания миграции локально в appsettings.json заполните поля
"ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=MinecraftSkinsDb;Username=postgres;Password=ваш пароль"
  },
  и
  "Redis": {
    "Configuration": "localhost:6379,password=ваш пароль для redis"
  },
  После применения миграции рекомендуется убрать эти данные из appsettings.json и оставить строки пустыми.

---

## Миграции

- **При запуске API** миграции применяются автоматически (`db.Database.Migrate()` в `Program.cs`).
- **Вручную** (из корня решения):
  ```bash
  Update-Database -Project MinecraftSkins.Infrastructure -StartupProject MinecraftSkins.Api -Context AppDbContext
  ```
- **Добавить новую миграцию:**
  ```bash
  Add-Migration имяМиграции -Project MinecraftSkins.Infrastructure -OutputDir "Data/Migrations" -StartupProject MinecraftSkins.Api -Context AppDbContext
  ```

В проекте одна начальная миграция `Initial`: схема (Users, Roles, UserRoles, Skins, Purchases), роли и 20 скинов в seed. Пользователи (TestUser, Admin, TestUser2) создаются при старте через `DataSeeder` (пароли хешируются через ASP.NET Core Identity).

---

## Формула расчёта цены

Финальная цена скина в USD считается в отдельном компоненте **`IPriceCalculator`** (реализации: `StandardPriceCalculator`, `PromoPriceCalculator`), чтобы формулу можно было менять без контроллеров.

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

---

## API (endpoints)

### Skins

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/skins` | Список скинов. Параметры: `availableOnly`, `search`, `skip`, `take`. В ответе — базовые поля и рассчитанная финальная цена (и курс при доступности). |
| GET | `/api/skins/{id}` | Детали скина и финальная цена. |
| POST | `/api/skins` | Создать скин (Name, BasePriceUsd, IsAvailable). **Требуется роль Admin.** |
| PUT | `/api/skins/{id}` | Обновить скин. **Требуется роль Admin.** |
| DELETE | `/api/skins/{id}` | Удаление (soft delete). **Требуется роль Admin.** |

### Rates

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/rates/btc-usd` | Текущий курс BTC/USD, метаданные: `rate`, `asOfUtc`, `source` (Cache/External/Fallback), `ageSeconds`. **Требуется роль Admin.** При недоступности внешнего API и отсутствии fallback — 503. |

### Purchases

| Метод | Путь | Описание |
|-------|------|----------|
| POST | `/api/purchases` | Покупка скина. Тело: `{ "skinId": "guid" }`. Авторизация по JWT (BuyerId из токена). Поддерживается идемпотентность: заголовок `Idempotency-Key`. Ответ: созданный чек (id, skinId, finalPrice, rate, purchasedAt). 201 / 400 / 404 / 409 / 503. |
| GET | `/api/purchases` | Список чеков. Параметры: `buyerId`, `skinId`, `from`, `to`, `skip`, `take`. Для текущего пользователя можно не передавать `buyerId`. **Требуется авторизация.** |
| GET | `/api/purchases/{id}` | Чек по id. **Требуется авторизация.** |

### Auth

| Метод | Путь | Описание |
|-------|------|----------|
| POST | `/api/login` | Вход: `{ "userName", "password" }` → JWT. |
| POST | `/api/register` | Регистрация. |

### Health

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/health` | Health checks: БД, провайдер курса BTC, Redis. |

---

## Формат ошибок

Используется **ProblemDetails** (RFC 7807):

- Валидация (FluentValidation) → **400** с деталями в `errors`.
- Не найден ресурс → **404**.
- Скин недоступен для покупки / конфликт при конкурентном обновлении → **409**.
- Внешний курс недоступен и нет fallback → **503**.
- В ответе также передаётся `traceId` для корреляции.

---

## Архитектура

Слои разделены по проектам и ответственности; контроллеры (endpoints) тонкие, без прямой работы с DbContext и без бизнес-логики.

| Проект | Назначение |
|--------|------------|
| **MinecraftSkins.Api** | Точка входа: Minimal API (MapGroup), middleware, Swagger, composition root (DI), глобальная обработка ошибок (ProblemDetails), фильтры (идемпотентность), health checks. |
| **MinecraftSkins.Application** | Use-cases: сервисы (SkinService, PurchaseService, BtcRateService), интерфейсы репозиториев и внешних клиентов (IBtcRateService), **расчёт цены** (IPriceCalculator, Standard/Promo), DTO, FluentValidation, опции (PriceCalculator, BtcRateProvider). Вся бизнес-логика покупки и расчёта цены — здесь. |
| **MinecraftSkins.Domain** | Сущности (Skin, Purchase), модели (BtcRateResult), интерфейсы репозиториев и провайдеров (IBtcRateProvider), доменные контракты. |
| **MinecraftSkins.Infrastructure** | EF Core DbContext, миграции, реализации репозиториев, **HTTP-клиенты внешних API** (CoinGecko, Binance — через HttpClientFactory, типизированные клиенты), кэш (IMemoryCache + Redis в BtcRateService), seed скинов и ролей, DataSeeder пользователей. |

**Ключевые решения:**

- **Цена:** расчёт вынесен в `IPriceCalculator` (Application), тестируемо и заменяемо; две стратегии (Standard/Promo) через конфиг/DI.
- **Курс BTC:** интерфейс `IBtcRateProvider` (Domain), реализации в Infrastructure; слой Application — `IBtcRateService` с кэшем (Memory + Redis), fallback на последнее успешное значение (до 10 минут), иначе 503.
- **Покупка:** проверка существования/доступности скина, получение курса, расчёт цены, создание Purchase — в Application; оптимистичная конкуренция (RowVersion на Skin), при конфликте — повторное чтение и при необходимости 409.
- **Идемпотентность:** заголовок `Idempotency-Key` на POST `/api/purchases`, хранение результата в Redis.
- **Авторизация:** JWT; BuyerId берётся из токена. Роли Admin (управление скинами, endpoint курса) и User.

**EF Core:** DbContext и миграции в Infrastructure; AsNoTracking для каталогов, пагинация и фильтрация в репозиториях, soft delete через query filter на Skin, RowVersion для оптимистичной конкуренции.

---

## Конфигурация

- **appsettings.json** — строка подключения к БД, Redis, JWT Secret, CORS, провайдер курса (`BtcRateProvider:Provider`: CoinGecko/Binance), опции калькулятора цены.
- **Переменные окружения** переопределяют настройки (в т.ч. в Docker: `ConnectionStrings__DefaultConnection`, `Redis__Configuration`, `ApiSettings__Secret` и т.д.).
- **.env** в корне используется для docker-compose (DB_*, REDIS_*, JWT_SECRET, VITE_API_BASE_URL). Для локального запуска бэкенда можно использовать свой appsettings или переменные окружения.