# MinecraftSkins

Сервис продажи Minecraft-скинов с привязкой цены к курсу BTC/USD.  
Backend: ASP.NET Core 8, Minimal API. Frontend: React + Vite (см. [minecraftskins.front](minecraftskins.front/)).

## Запуск через docker-compose

В корне репозитория создайте файл **.env** (можно скопировать из примера ниже) и заполните пароли и секреты.

### Переменные окружения (.env)

```env
# База данных
DB_HOST=localhost
DB_NAME=MinecraftSkinsDb
DB_USER=postgres
DB_PASSWORD=ваш_пароль

# Redis
REDIS_HOST=localhost
REDIS_PORT=6379
REDIS_PASSWORD=ваш_пароль_redis

# JWT
JWT_SECRET=ваш_длинный_секрет_для_подписи_токенов

# Фронтенд (опционально): базовый URL API с точки зрения браузера.
# По умолчанию при сборке фронта подставляется http://localhost:8080/api.
# HTTP:  http://localhost:8080/api
# HTTPS: https://localhost:8081/api
# Другой хост: VITE_API_BASE_URL=http://192.168.1.10:8080/api
# В .env пишите значение без пробелов и без точки с запятой в конце (иначе попадёт в значение).
# VITE_API_BASE_URL=
```

Запуск:

```bash
docker-compose up --build
```

- **Фронтенд:** http://localhost:3000  
- **API (HTTP):** http://localhost:8080 (Swagger: http://localhost:8080/swagger)  
- **API (HTTPS):** https://localhost:8081 (Swagger: https://localhost:8081/swagger)  
- **PostgreSQL:** localhost:5432  
- **Redis:** localhost:6379  

Если используете HTTPS для API, в **.env** задайте `VITE_API_BASE_URL=https://localhost:8081/api` (без пробела и точки с запятой в конце) и пересоберите образ фронта: `docker-compose up --build`.  

На бэкенде в `appsettings.json` в **Cors:AllowedOrigins** указаны `http://localhost:3000` и `http://localhost:5173`. Если открываете фронт с другого адреса, добавьте его в CORS (или переопределите через переменные окружения для сервиса **api**, см. [minecraftskins.front/docs/06-zapusk-i-konfiguraciya.md](minecraftskins.front/docs/06-zapusk-i-konfiguraciya.md)).

## Локальный запуск без Docker

- **Backend:** из папки решения запустите MinecraftSkins.Api (нужны PostgreSQL, Redis и переменные окружения / appsettings).
- **Frontend:** см. [minecraftskins.front/README.md](minecraftskins.front/README.md). В корне `minecraftskins.front` создайте `.env` с `VITE_DEV_API_ORIGIN=http://localhost:8080` (или порт вашего API), затем `npm install` и `npm run dev`.

Документация по фронтенду (структура, файлы, жизненный цикл, пользователи): [minecraftskins.front/docs/](minecraftskins.front/docs/).
