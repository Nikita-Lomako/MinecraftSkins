# Сборка фронтенда (Vite + React)
FROM node:20-alpine AS build
WORKDIR /app

# Кэширование зависимостей
COPY package.json package-lock.json* ./
RUN npm ci

# Сборка: базовый URL API задаётся при сборке образа (docker-compose build --build-arg)
ARG VITE_API_BASE_URL=http://localhost:8080/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

COPY . .
RUN npm run build

# Раздача статики через nginx
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
# SPA: все пути ведут на index.html
RUN echo 'server { root /usr/share/nginx/html; index index.html; location / { try_files $uri $uri/ /index.html; } }' > /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
