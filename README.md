# ResX — Resource Crossing Platform

> P2P-платформа для сокращения твёрдых бытовых отходов: горожане и НКО могут бесплатно отдавать, обменивать и жертвовать вещи, которые ещё можно использовать.

---

## Содержание

- [Обзор](#обзор)
- [Архитектура](#архитектура)
- [Сервисы](#сервисы)
- [Tech Stack](#tech-stack)
- [Требования](#требования)
- [Быстрый старт (Docker Compose)](#быстрый-старт-docker-compose)
- [Деплой на Timeweb Cloud App Platform](#деплой-на-timeweb-cloud-app-platform)
- [Seeded-данные](#seeded-данные)
- [Переменные окружения](#переменные-окружения)
- [API Endpoints](#api-endpoints)
- [Доменные модели](#доменные-модели)
- [Эко-метрики](#эко-метрики)
- [Стратегия кэширования](#стратегия-кэширования)
- [Интеграционные тесты](#интеграционные-тесты)
- [Рабочий процесс разработки](#рабочий-процесс-разработки)
- [Структура проекта](#структура-проекта)
- [Участие в разработке](#участие-в-разработке)

---

## Обзор

**ResX** (ресурс-кроссинг) — backend-платформа, которая соединяет людей, у которых есть ненужные вещи, с теми, кто в них нуждается. Цель — сокращение свалочных отходов и формирование циклической экономики в городах.

**Ключевые возможности:**
- Публикация объявлений: бесплатная отдача, обмен или благотворительное пожертвование
- Обязательный вес товара при создании объявления + автоматический расчёт эко-эффекта по ставке категории
- Обмен сообщениями (с обогащением данными о собеседнике и листинге)
- Благотворительные кампании НКО с процессом верификации
- Разрешение споров по несостоявшимся транзакциям
- Загрузка файлов (фото) в S3-совместимое объектное хранилище с pre-signed URL
- Накопительная эко-статистика на профиле (CO₂ сэкономлено, отходов отведено от свалки)
- Аналитика платформы: тренды по категориям, активность по городам
- JWT-аутентификация с ротацией refresh-токенов и Redis-блэклистом

---

## Архитектура

```
                           +-------------------------------------+
                           |          Клиентские приложения      |
                           |   (Web, Mobile, Third-party APIs)   |
                           +----------------+--------------------+
                                            | HTTPS
                           +----------------v--------------------+
                           |           API Gateway               |
                           |        (YARP Reverse Proxy)         |
                           |     JWT validation . Rate Limit     |
                           +--+--+--+--+--+--+--+--+--+--+------+
                              |  |  |  |  |  |  |  |  |  |
          +-------------------+  |  |  |  |  |  |  |  |  +-------------------+
          |           +----------+  |  |  |  |  |  |  +----------+           |
          v           v             v  |  |  v  v             v  v           v
   +----------+ +----------+ +-------+ |  | +-----+ +----------+ +----------+
   | Identity | |  Users   | |Listing| |  | |Trans| |Messaging | |  Files   |
   | Service  | | Service  | |Service| |  | |Svc  | | Service  | | Service  |
   +----------+ +----------+ +-------+ |  | +-----+ +----------+ +----------+
                                       |  |
                    +------------------+  +------------------+
                    v                                        v
             +----------+  +----------+  +----------+ +----------+
             |Notifica- |  | Charity  |  |Disputes  | |Analytics |
             |  tions   |  | Service  |  | Service  | | Service  |
             +----------+  +----------+  +----------+ +----------+

                    +-------------- Инфраструктура ----------------+
                    |  PostgreSQL . RabbitMQ . Redis . MinIO/S3   |
                    +----------------------------------------------+
```

**Паттерны коммуникации:**
- **HTTP/REST** — клиент ↔ API Gateway ↔ сервисы (порт `8080` контейнера, HTTP/1.1).
- **gRPC** — синхронные межсервисные вызовы по plaintext HTTP/2. Сервисы с gRPC-сервером (`Users`, `Listings`) слушают **второй** порт `8081` отдельно от REST на `8080`.
  - `Messaging → Users:8081` (имя/аватар собеседника), `Messaging → Listings:8081` (заголовок объявления).
  - `Listings → Users:8081` (имя/аватар донора).
  - `Transactions → Listings:8081` (eco-данные листинга при завершении сделки).
- **RabbitMQ** — асинхронные интеграционные события: `UserRegistered`, `ListingCreated`, `TransactionCompleted`, `TransactionCancelled`, `MessageSent` и др. Маршрутизация — по имени типа события.
- **WebSocket / SignalR** — заявлен для real-time чата (хаб мапится в `messaging-service`), но реальная подписка фронтенда пока опциональна — обновления видны через REST-polling.

**Архитектура каждого сервиса:** Clean Architecture + DDD
```
ResX.<Service>.Domain          ← Aggregates, Value Objects, Domain Events
ResX.<Service>.Application     ← Commands, Queries (CQRS/MediatR), DTOs, интерфейсы репозиториев
ResX.<Service>.Infrastructure  ← EF Core DbContext, реализации репозиториев, FluentMigrator
ResX.<Service>.API             ← ASP.NET Core Controllers, Program.cs
```

---

## Сервисы

| Сервис               | Внутр. HTTP | Внутр. gRPC | Dev-порт | Описание                                          |
|----------------------|:-----------:|:-----------:|:--------:|---------------------------------------------------|
| **API Gateway**      | 8080        | —           | 8080     | YARP reverse proxy, JWT-аутентификация, rate limit|
| **Identity Service** | 8080        | —           | 5001     | Регистрация, вход, выдача JWT, refresh-токены     |
| **Users Service**    | 8080        | 8081        | 5002     | Профили, репутация, отзывы, эко-статистика        |
| **Listings Service** | 8080        | 8081        | 5003     | Объявления, поиск, категории, status machine, веса/эко|
| **Transactions Svc** | 8080        | —           | 5004     | Жизненный цикл транзакций, подтверждения, споры   |
| **Messaging Service**| 8080        | —           | 5005     | Диалоги, сообщения, отметки прочитанными          |
| **Notifications Svc**| 8080        | —           | 5006     | Внутренние уведомления через consumption событий  |
| **Charity Service**  | 8080        | —           | 5007     | НКО-организации, благотворительные заявки         |
| **Disputes Service** | 8080        | —           | 5008     | Споры, доказательства, решение модератора         |
| **Files Service**    | 8080        | —           | 5009     | Загрузка/скачивание файлов через S3, pre-signed URLs|
| **Analytics Service**| 8080        | —           | 5010     | Эко-статистика, тренды по категориям, активность по городам|

**Инфраструктура:**

| Компонент    | Dev-порт | Назначение                                  |
|--------------|:--------:|---------------------------------------------|
| PostgreSQL   | 5432     | Основное хранилище (отдельная БД на сервис) |
| RabbitMQ     | 5672     | Message broker                              |
| RabbitMQ UI  | 15672    | Management console (`guest` / `guest`)      |
| Redis        | 6379     | Распределённый кэш и блэклист токенов       |
| MinIO        | 9000     | S3-совместимое объектное хранилище (S3 API) |
| MinIO UI     | 9001     | Web-консоль MinIO (`minioadmin` / `minioadmin123`)|

---

## Tech Stack

| Слой              | Технология                                          |
|-------------------|-----------------------------------------------------|
| Язык              | C# 13 / .NET 10                                     |
| Web Framework     | ASP.NET Core 10                                     |
| API Gateway       | YARP (Yet Another Reverse Proxy) 2.3                |
| ORM               | Entity Framework Core 10 (Npgsql provider)          |
| Миграции          | FluentMigrator                                      |
| CQRS/Mediator     | MediatR 12                                          |
| Валидация         | FluentValidation                                    |
| Сериализация      | System.Text.Json (camelCase)                        |
| Обмен сообщениями | RabbitMQ + кастомный EventBus building block        |
| RPC               | gRPC (Google.Protobuf, Grpc.AspNetCore) — plaintext HTTP/2|
| Кэширование       | Redis (StackExchange.Redis)                         |
| Объектное хранилище| S3 (AWSSDK.S3 — MinIO, self-hosted)                |
| Аутентификация    | JWT Bearer (Microsoft.AspNetCore.Authentication)    |
| API-документация  | Swagger / OpenAPI (Swashbuckle) с required-полями из NRT|
| Health Checks     | ASP.NET Core HealthChecks + Npgsql                  |
| Контейнеризация   | Docker (multi-stage builds), Docker Compose         |
| База данных       | PostgreSQL 17                                       |
| Тестирование      | xUnit, FluentAssertions, Testcontainers, NSubstitute, Respawner|

---

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (с Docker Compose v2)

---

## Быстрый старт (Docker Compose)

В репозитории **два compose-файла**:

| Файл                       | Назначение                                                                |
|----------------------------|---------------------------------------------------------------------------|
| `docker-compose.yml`       | **Облачный** — для Timeweb App Platform. Только бэкенд-сервисы + gateway, инфраструктура (Postgres / RabbitMQ / Redis / S3) внешняя. |
| `docker-compose.local.yml` | **Локальный** — полный стек с Postgres / RabbitMQ / Redis / MinIO в контейнерах и volumes для данных.|

Для локальной разработки используй `docker-compose.local.yml` (флагом `-f`).

### 1. Клонирование репозитория

```bash
git clone https://github.com/your-org/resx.git
cd resx
```

### 2. Запуск всех сервисов локально

```bash
docker compose -f docker-compose.local.yml up -d --build
```

Первая сборка — 5–15 минут. Дальше слои кэшируются, рестарт одного сервиса — секунды.

Точечная пересборка одного сервиса:

```bash
docker compose -f docker-compose.local.yml up -d --build listings-service
```

> **Не запускай `docker compose up` без `-f`** — это поднимет облачную конфигурацию, у которой нет ни Postgres, ни Redis, ни MinIO. Все сервисы лягут на старте с ошибкой подключения.

### 4. Проверка

| URL                                    | Описание                        |
|----------------------------------------|---------------------------------|
| http://localhost:8080/health           | Health check API Gateway        |
| http://localhost:5001/swagger          | Swagger UI — Identity Service   |
| http://localhost:5002/swagger          | Swagger UI — Users Service      |
| http://localhost:5003/swagger          | Swagger UI — Listings Service   |
| http://localhost:5004/swagger          | Swagger UI — Transactions Service|
| http://localhost:5005/swagger          | Swagger UI — Messaging Service  |
| http://localhost:5006/swagger          | Swagger UI — Notifications Service|
| http://localhost:5007/swagger          | Swagger UI — Charity Service    |
| http://localhost:5008/swagger          | Swagger UI — Disputes Service   |
| http://localhost:5009/swagger          | Swagger UI — Files Service      |
| http://localhost:5010/swagger          | Swagger UI — Analytics Service  |
| http://localhost:15672                 | RabbitMQ Management UI          |
| http://localhost:9001                  | MinIO Console                   |

### 5. Остановка

```bash
docker compose -f docker-compose.local.yml down
# Удалить также volumes (все данные будут удалены):
docker compose -f docker-compose.local.yml down -v
```

---

## Деплой на Timeweb Cloud App Platform

App Platform Timeweb запрещает `volumes:`, хост-порты `80/443`, `network_mode: host` и ряд других директив. **Состояние хранить нельзя — БД, очереди, объектное хранилище должны быть внешними.** Поэтому корневой `docker-compose.yml` адаптирован: содержит только бэкенд-сервисы + gateway, а инфраструктура подключается по env-переменным.

### Шаг 1. Поднять внешнюю инфраструктуру в Timeweb

В панели Timeweb Cloud:

1. **PostgreSQL (DBaaS)** — один кластер, 9 баз внутри. После создания подключись через psql или встроенный SQL-редактор и выполни:
   ```sql
   CREATE DATABASE resx_identity;
   CREATE DATABASE resx_users;
   CREATE DATABASE resx_listings;
   CREATE DATABASE resx_transactions;
   CREATE DATABASE resx_messaging;
   CREATE DATABASE resx_notifications;
   CREATE DATABASE resx_charity;
   CREATE DATABASE resx_disputes;
   CREATE DATABASE resx_files;
   ```
   Готовый список — в [scripts/init-databases.sql](scripts/init-databases.sql).
2. **Redis (DBaaS)** — один экземпляр, подключение по TLS.
3. **RabbitMQ** — managed-сервиса нет, поднимай на отдельном Cloud Server (VPS) или используй managed-RabbitMQ другого провайдера. Не забудь создать vhost `resx`.
4. **Object Storage** — раздел «Объекты (S3)» → создай бакет `resx-files`. Из виджета внизу бакета забери: endpoint (`https://s3.twcstorage.ru`), Access Key, Secret Key, регион (`ru-1`).

### Шаг 2. Заполнить env-переменные

Скопируй [.env.example](.env.example) → `.env` локально (для проверок) и **обязательно** перенеси значения в **«Переменные окружения»** в настройках приложения на App Platform — оттуда оно подставит их при сборке. В Git **`.env` не коммитим** — он в `.gitignore`.

Ключевые группы:

| Группа    | Переменные                                                              |
|-----------|-------------------------------------------------------------------------|
| JWT       | `JWT_SECRET_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`                          |
| CORS      | `FRONTEND_ORIGIN` (домен фронта, на который gateway разрешит запросы)   |
| Postgres  | `<SERVICE>_DB_CONNECTION` × 9                                           |
| Redis     | `REDIS_CONNECTION`                                                      |
| RabbitMQ  | `RABBIT_HOST`, `RABBIT_PORT`, `RABBIT_VHOST`, `RABBIT_USER`, `RABBIT_PASSWORD` |
| S3        | `S3_SERVICE_URL`, `S3_PUBLIC_URL`, `S3_ACCESS_KEY`, `S3_SECRET_KEY`, `S3_BUCKET_NAME`, `S3_REGION` |

### Шаг 3. Создать приложение в App Platform

1. Панель Timeweb Cloud → **App Platform** → **«Создать приложение»**.
2. Тип: **Docker → Docker Compose**.
3. Подключи GitHub-репозиторий с этим проектом, выбери ветку.
4. Регион и тариф подбирай по своей нагрузке.
5. В разделе **«Переменные окружения»** вставь значения из `.env`.
6. Запусти деплой. Платформа прочитает `docker-compose.yml`, соберёт все 11 образов (gateway + 10 микросервисов) и стартанёт их.

### Шаг 4. Проверка после деплоя

- Основной домен App Platform указывает на **`api-gateway`** (он первый в `services:`). Проверь:
  ```
  https://<your-app>.tw1.ru/health  → 200
  https://<your-app>.tw1.ru/api/categories  → JSON-список категорий
  ```
- Мониторь логи каждого сервиса в панели — особенно подключения к Postgres / Redis / RabbitMQ на старте. Миграции FluentMigrator накатываются автоматически при первом запуске.
- Swagger остальных микросервисов **не доступен снаружи** — ports у них убраны. Это намеренно. Доступ только через gateway. Для дебага временно добавь `ports: ["5001:8080"]` к нужному сервису.

### Шаг 5. Привязка домена и фронта

- В App Platform → твоё приложение → «Домены» — привяжи `api.your-domain.com`.
- Во фронте установи `VITE_API_URL` (или аналог) на этот домен.
- В env бэкенда переопредели `FRONTEND_ORIGIN=https://app.your-domain.com` — gateway разрешит CORS только с этого источника.

### Ограничения, о которых нужно помнить

- **Без volumes** — никаких локальных файлов: всё в Postgres / Object Storage. Не пытайся ставить `volumes:` в `docker-compose.yml` — деплой упадёт.
- **Без host-портов 80/443** — у gateway внутренний `8080`, наружу его пробрасывает сам Timeweb на HTTPS.
- **Без `network_mode: host`** — не нужен, межсервисные вызовы идут по имени контейнера в compose-сети.
- **Поднять Postgres / Redis на паузу нельзя** — DBaaS Timeweb тарифицируется почасово, остановить кластер без удаления нельзя. Для длинных пауз: дамп через `pg_dump`, удаление кластера, при возобновлении — новый кластер + восстановление дампа.

---

## Seeded-данные

При первом запуске Identity-сервиса миграция `M003_SeedAdminUser` создаёт администратора:

```
Email:    admin@resx.local
Password: Admin123!
Role:     Admin
```

**Обязательно смени пароль через `PUT /api/auth/change-password` перед прод-запуском.**

Listings-сервис при первом запуске создаёт 5 категорий с реалистичными эко-ставками (CO₂ и waste на 100 г товара):

| Категория      | CO₂/100g | Waste/100g |
|----------------|:--------:|:----------:|
| Clothing       | 300 г    | 100 г      |
| Electronics    | 600 г    | 100 г      |
| Furniture      | 100 г    | 100 г      |
| Books          | 30 г     | 100 г      |
| Toys & Games   | 50 г     | 100 г      |

Ставки потом можно подкручивать через `PUT /api/categories/{id}` от имени админа.

---

## Переменные окружения

### Общие (все сервисы)

| Переменная                             | Описание                               | По умолчанию (dev)          |
|----------------------------------------|----------------------------------------|-----------------------------|
| `ASPNETCORE_ENVIRONMENT`               | Среда выполнения                       | `Development`               |
| `ConnectionStrings__<Service>Db`       | Строка подключения к Postgres          | `Host=postgres;Database=...`|
| `Jwt__SecretKey`                       | Ключ подписи JWT (минимум 32 символа)  | —                           |
| `Jwt__Issuer`                          | Издатель JWT                           | `ResX.Identity`             |
| `Jwt__Audience`                        | Аудитория JWT                          | `ResX`                      |
| `Jwt__AccessTokenExpiryMinutes`        | TTL access-токена (только Identity)    | `60`                        |
| `Jwt__RefreshTokenExpiryDays`          | TTL refresh-токена (только Identity)   | `30`                        |
| `EventBus__HostName`                   | Хост RabbitMQ                          | `rabbitmq`                  |
| `EventBus__VirtualHost`                | RabbitMQ vhost                         | `resx`                      |
| `EventBus__UserName` / `EventBus__Password` | RabbitMQ credentials              | `guest` / `guest`           |
| `ConnectionStrings__Redis`             | Строка подключения к Redis             | `redis:6379,password=...`   |
| `Logging__LogLevel__Default`           | Уровень логирования                    | `Information` / `Debug` в overrides|

### Межсервисные gRPC

| Переменная                        | Где                | Куда указывает                   |
|-----------------------------------|--------------------|----------------------------------|
| `Services__Users__GrpcUrl`        | Listings, Messaging| `http://users-service:8081`      |
| `Services__Listings__GrpcUrl`     | Messaging, Transactions | `http://listings-service:8081` |

### Только Files Service

| Переменная       | Описание                                                     | По умолчанию (dev)     |
|------------------|--------------------------------------------------------------|------------------------|
| `S3__ServiceUrl` | Внутренний URL S3 (для backend → MinIO в Docker-сети)        | `http://minio:9000`    |
| `S3__PublicUrl`  | Browser-доступный URL для presigned URL                      | `http://localhost:9000`|
| `S3__AccessKey`  | Access key S3                                                | `minioadmin`           |
| `S3__SecretKey`  | Secret key S3                                                | `minioadmin123`        |
| `S3__BucketName` | Имя bucket S3                                                | `resx-files`           |
| `S3__Region`     | Регион S3-клиента (MinIO игнорирует)                         | `us-east-1`            |

Для прод-сервера переопределяй `S3_PUBLIC_URL` через `.env`: `S3_PUBLIC_URL=https://files.your-domain.com` (MinIO стоит за nginx с TLS).

### Только Analytics Service

| Переменная                           | Описание                                  |
|--------------------------------------|-------------------------------------------|
| `ConnectionStrings__UsersDb`         | Read-подключение к БД Users               |
| `ConnectionStrings__ListingsDb`      | Read-подключение к БД Listings            |
| `ConnectionStrings__TransactionsDb`  | Read-подключение к БД Transactions        |

---

## API Endpoints

Все endpoint'ы доступны через API Gateway: `http://localhost:8080` (dev) или прямо к сервису по dev-порту (для дебага).

### Identity Service `/api/auth`

| Метод | Путь                        | Auth | Описание                         |
|-------|-----------------------------|------|----------------------------------|
| POST  | `/api/auth/register`        | —    | Регистрация нового пользователя  |
| POST  | `/api/auth/login`           | —    | Вход, выставляет access/refresh cookies |
| POST  | `/api/auth/refresh`         | —    | Обновление access-токена         |
| POST  | `/api/auth/logout`          | JWT  | Отзыв refresh-токена             |
| PUT   | `/api/auth/change-password` | JWT  | Смена пароля                     |

### Users Service `/api/users`

| Метод | Путь                             | Auth  | Описание                            |
|-------|----------------------------------|-------|-------------------------------------|
| GET   | `/api/users/me`                  | JWT   | Профиль текущего пользователя       |
| GET   | `/api/users/{id}`                | —     | Получить профиль пользователя       |
| PUT   | `/api/users/me`                  | JWT   | Обновить собственный профиль        |
| PUT   | `/api/users/me/avatar`           | JWT   | Обновить аватар                     |
| GET   | `/api/users/{id}/reviews`        | —     | Получить отзывы о пользователе      |
| POST  | `/api/users/{id}/reviews`        | JWT   | Оставить отзыв о пользователе       |
| GET   | `/api/users/eco-leaderboard`     | —     | Эко-рейтинг пользователей           |

### Listings Service `/api/listings`, `/api/categories`

| Метод  | Путь                            | Auth  | Описание                                  |
|--------|---------------------------------|-------|-------------------------------------------|
| GET    | `/api/listings`                 | —     | Поиск/фильтрация Active-объявлений (кэш)  |
| GET    | `/api/listings/my`              | JWT   | Свои объявления (все статусы)             |
| GET    | `/api/listings/{id}`            | —     | Детали объявления (включая `co2SavedG`, `wasteSavedG`)|
| POST   | `/api/listings`                 | JWT   | Создать объявление — **`weightGrams` обязателен** |
| PUT    | `/api/listings/{id}`            | JWT   | Обновить своё объявление                  |
| DELETE | `/api/listings/{id}`            | JWT   | Удалить своё объявление                   |
| PATCH  | `/api/listings/{id}/status`     | JWT   | Изменить статус (например, Draft → Active)|
| POST   | `/api/listings/{id}/photos`     | JWT   | Добавить фото к объявлению                |
| GET    | `/api/categories`               | —     | Список активных категорий + эко-ставки    |
| POST   | `/api/categories`               | Admin | Создать категорию                         |
| PUT    | `/api/categories/{id}`          | Admin | Обновить категорию (имя, иконка, эко-ставки)|
| DELETE | `/api/categories/{id}`          | Admin | Деактивировать категорию (soft-delete)    |
| GET    | `/api/categories/{id}/history`  | Admin | Журнал изменений категории                |

### Transactions Service `/api/transactions`

| Метод | Путь                                     | Auth | Описание                                      |
|-------|------------------------------------------|------|-----------------------------------------------|
| GET   | `/api/transactions`                      | JWT  | Мои транзакции                                |
| POST  | `/api/transactions`                      | JWT  | Создать транзакцию (вызывает получатель)      |
| GET   | `/api/transactions/{id}`                 | JWT  | Детали транзакции                             |
| POST  | `/api/transactions/{id}/agree`           | JWT  | Донор соглашается (Pending → DonorAgreed)     |
| POST  | `/api/transactions/{id}/confirm-receipt` | JWT  | Получатель подтверждает (DonorAgreed → Completed) → начисление эко-статистики|
| POST  | `/api/transactions/{id}/cancel`          | JWT  | Отменить транзакцию (любой участник)          |
| POST  | `/api/transactions/{id}/dispute`         | JWT  | Открыть спор по транзакции                    |

### Messaging Service `/api/messaging`

| Метод | Путь                                          | Auth | Описание                       |
|-------|-----------------------------------------------|------|--------------------------------|
| GET   | `/api/messaging/conversations`                | JWT  | Список диалогов (с обогащением: counterparty, listing summary, lastMessage, unreadCount) |
| POST  | `/api/messaging/conversations`                | JWT  | Начать новый диалог            |
| GET   | `/api/messaging/conversations/{id}/messages`  | JWT  | Сообщения диалога              |
| POST  | `/api/messaging/conversations/{id}/messages`  | JWT  | Отправить сообщение            |
| POST  | `/api/messaging/conversations/{id}/read`      | JWT  | Отметить чужие сообщения прочитанными|

### Notifications Service `/api/notifications`

| Метод | Путь                                   | Auth | Описание                         |
|-------|----------------------------------------|------|----------------------------------|
| GET   | `/api/notifications`                   | JWT  | Получить уведомления (с пагинацией)|
| POST  | `/api/notifications/{id}/read`         | JWT  | Отметить уведомление прочитанным |
| POST  | `/api/notifications/read-all`          | JWT  | Отметить все уведомления прочитанными|

### Charity Service `/api/charity`

| Метод | Путь                                          | Auth  | Описание                                   |
|-------|-----------------------------------------------|-------|--------------------------------------------|
| GET   | `/api/charity/requests`                       | —     | Список активных благотворительных заявок   |
| GET   | `/api/charity/requests/{id}`                  | —     | Детали благотворительной заявки            |
| POST  | `/api/charity/requests`                       | JWT   | Создать заявку (от лица своей организации) |
| POST  | `/api/charity/requests/{id}/cancel`           | Admin | Отменить благотворительную заявку          |
| POST  | `/api/charity/requests/{id}/complete`         | Admin | Завершить благотворительную заявку         |
| GET   | `/api/charity/organizations/{id}`             | —     | Данные организации                         |
| POST  | `/api/charity/organizations`                  | JWT   | Зарегистрировать новую организацию         |
| PUT   | `/api/charity/organizations/{id}/verify`      | Admin | Верифицировать организацию                 |
| PUT   | `/api/charity/organizations/{id}/reject`      | Admin | Отклонить верификацию организации          |

### Disputes Service `/api/disputes`

| Метод | Путь                            | Auth                | Описание                              |
|-------|---------------------------------|---------------------|---------------------------------------|
| GET   | `/api/disputes`                 | JWT                 | Свои споры; Admin/Moderator — все      |
| POST  | `/api/disputes`                 | JWT                 | Открыть новый спор                    |
| GET   | `/api/disputes/{id}`            | JWT                 | Детали спора с доказательствами       |
| POST  | `/api/disputes/{id}/evidence`   | JWT                 | Добавить доказательство к спору       |
| POST  | `/api/disputes/{id}/resolve`    | Moderator/Admin     | Разрешить спор с выводом              |
| POST  | `/api/disputes/{id}/close`      | Moderator/Admin     | Закрыть спор без решения              |
| GET   | `/api/disputes/open`            | Moderator/Admin     | Список открытых/рассматриваемых споров|

### Files Service `/api/files`

| Метод  | Путь                   | Auth | Описание                              |
|--------|------------------------|------|---------------------------------------|
| POST   | `/api/files/upload`    | JWT  | Загрузить файл (multipart/form-data)  |
| GET    | `/api/files/{id}/url`  | JWT  | Получить pre-signed URL для скачивания (использует `S3__PublicUrl`)|
| DELETE | `/api/files/{id}`      | JWT  | Удалить свой файл                     |

### Analytics Service `/api/analytics`

| Метод | Путь                          | Auth | Описание                              |
|-------|-------------------------------|------|---------------------------------------|
| GET   | `/api/analytics/eco-stats`    | —    | Эко-статистика всей платформы         |
| GET   | `/api/analytics/categories`   | —    | Статистика активности по категориям   |
| GET   | `/api/analytics/cities`       | —    | Разбивка активности по городам        |

---

## Доменные модели

### Status Machine объявлений

Объявления создаются в статусе **Draft**. Владелец должен явно опубликовать их.

```
Draft ──────────────────────────────────► Cancelled
  │
  ▼
Active ──► Reserved ──► Completed
  │            │
  ▼            ▼
Cancelled   Cancelled
  ▲
Moderated ──► Active
  │
  ▼
Cancelled
```

| Переход                 | Инициатор               |
|-------------------------|-------------------------|
| Draft → Active          | Владелец (публикация)   |
| Draft → Cancelled       | Владелец                |
| Active → Reserved       | Владелец                |
| Active → Moderated      | Модератор               |
| Active → Cancelled      | Владелец                |
| Reserved → Completed    | Владелец                |
| Reserved → Active       | Владелец (снять резерв) |
| Reserved → Cancelled    | Владелец                |
| Moderated → Active      | Модератор (одобрение)   |
| Moderated → Cancelled   | Модератор (отклонение)  |

### Status Machine транзакций

Транзакцию инициирует **получатель**, запрашивая вещь у донора.

```
Pending ──► DonorAgreed ──► Completed
   │              │
   ▼              ▼
Cancelled      Cancelled
   │              │
   ▼              ▼
Disputed       Disputed
```

При переходе **`DonorAgreed → Completed`** Transactions публикует `TransactionCompletedIntegrationEvent` с предвычисленным `co2SavedG`/`wasteSavedG` (получены из Listings gRPC). Users подписан на это событие и инкрементит:
- Донору: `+1 itemsGifted`, `+co2SavedKg`, `+wasteSavedKg`.
- Получателю: `+1 itemsReceived` (без эко-савингов — они засчитываются только донору).

### Status Machine споров

```
Open ──► UnderReview ──► Resolved
  └───────────────────────────────► Closed
```

### Верификация организаций

```
Pending ──► Verified
    └──────► Rejected
```

---

## Эко-метрики

**Где живёт расчёт:**
- **Category** (Listings БД) хранит `co2SavedPer100GramsG` и `wasteSavedPer100GramsG` — ставки в граммах на каждые 100 г продукта. Управляется админом через `PUT /api/categories/{id}`.
- **Listing** при создании кэширует `weightGrams`, `co2SavedG = weight × catCo2 / 100`, `wasteSavedG = weight × catWaste / 100`. Историчность: пересчёт **не** ретроактивен — изменение ставки категории не трогает старые объявления.
- **UserProfile** (`EcoStats` value object) накапливает суммы по завершённым сделкам.

**Триггер инкремента — `TransactionCompletedIntegrationEvent`** (а не создание листинга). Это исключает накрутку через мёртвые объявления.

**На карточке листинга** показывается `co2SavedG` / `wasteSavedG` как **примерный** прогноз эко-эффекта при успешной сделке (фронт делит на 1000 → кг для отображения).

**На профиле пользователя** — фактическая накопленная статистика по завершённым transferам.

---

## Стратегия кэширования

Redis используется в трёх сервисах с разными подходами:

### Listings — version-based cache invalidation

Все ответы `GET /api/listings` кэшируются на **5 минут**. В Redis хранится монотонный счётчик версий (`listings:version`), который инкрементируется при любом изменении (создание, обновление, удаление, смена статуса, обновление категории). Ключ кэша содержит версию, поэтому устаревшие записи обходятся автоматически — без явного удаления.

```
Cache key: listings:v{version}:p{page}:s{size}:cat{catId}:cond{cond}:tr{type}:city{city}:q{query}
TTL: 5 минут
```

### Users — кэш профиля с точечной инвалидацией

Профили пользователей кэшируются на **10 минут**. Запись явно удаляется при любом изменении профиля (обновление, аватар, новый отзыв, обновление эко-статистики).

```
Cache key: users:profile:{userId}
TTL: 10 минут
Инвалидация: UpdateUserProfile, UpdateAvatar, AddReview, UpdateEcoStats
```

### Identity — блэклист refresh-токенов

Отозванные и ротированные refresh-токены добавляются в Redis-блэклист на **30 дней** (совпадает со сроком жизни токена). При каждом запросе на обновление токена блэклист проверяется до обращения к базе данных — это защищает от replay-атак с минимальными накладными расходами.

```
Cache key: identity:token:blacklist:{token}
TTL: 30 дней
Записывается: Logout, ротация токена в RefreshToken
```

---

## Интеграционные тесты

Для каждого сервиса есть отдельный проект интеграционных тестов, использующий реальный экземпляр PostgreSQL через **Testcontainers**. База данных сбрасывается между тестами с помощью **Respawner**. Внешние сервисы (gRPC, RabbitMQ, S3) замокированы через **NSubstitute**.

```
tests/
├── ResX.IntegrationTests.Common/       # Общие fixtures, JWT helpers, HTTP extensions
├── ResX.Identity.IntegrationTests/
├── ResX.Users.IntegrationTests/
├── ResX.Listings.IntegrationTests/
├── ResX.Transactions.IntegrationTests/
├── ResX.Messaging.IntegrationTests/
├── ResX.Notifications.IntegrationTests/
├── ResX.Charity.IntegrationTests/
├── ResX.Disputes.IntegrationTests/
├── ResX.Files.IntegrationTests/
└── ResX.Analytics.IntegrationTests/
```

### Запуск тестов

```bash
# Все интеграционные тесты (требуется Docker для Testcontainers)
dotnet test ResX.sln

# Тесты отдельного сервиса
dotnet test tests/ResX.Listings.IntegrationTests/
dotnet test tests/ResX.Transactions.IntegrationTests/
dotnet test tests/ResX.Messaging.IntegrationTests/
```

> Тесты автоматически поднимают контейнер PostgreSQL — ручная настройка не нужна.
>
> **Важно про конфиг:** в фикстурах env-переменные для gRPC-URLs (`Services__Users__GrpcUrl`, `Services__Listings__GrpcUrl`) выставляются **до** `CreateClient()`. `ConfigureAppConfiguration` в `WebApplicationFactory` срабатывает слишком поздно — `AddXxxInfrastructure` уже прочитал конфиг и упал бы с `InvalidOperationException` про отсутствующий ключ.

---

## Рабочий процесс разработки

### Локальная сборка (без Docker)

```bash
# Восстановить все пакеты
dotnet restore ResX.sln

# Собрать всё решение
dotnet build ResX.sln

# Запустить отдельный сервис (например, Listings)
cd src/Services/Listings/ResX.Listings.API
dotnet run
```

### Миграции базы данных

Каждый сервис запускает миграции FluentMigrator автоматически при старте. Миграции — **только аддитивные**: никогда не редактируйте существующие классы миграций, всегда создавайте новые.

### Добавление нового integration event

1. Определите класс события в папке `Application/IntegrationEvents/` публикующего сервиса.
2. Опубликуйте через `IEventBus.PublishAsync()` в обработчике команды.
3. Создайте обработчик в потребляющем сервисе, реализующий `IIntegrationEventHandler<TEvent>` — **класс события должен называться так же**: маршрутизация в шине идёт по `GetType().Name`.
4. Зарегистрируйте обработчик в DI: `services.AddScoped<MyHandler>()` в `Infrastructure/DependencyInjection.cs`.
5. Подпишитесь: `eventBus.Subscribe<MyEvent, MyHandler>()` в `API/Program.cs`.

### Добавление нового gRPC endpoint

1. Добавьте метод в соответствующий `.proto`-файл в `src/Protos/`.
2. На стороне сервера: реализуйте метод в `<Service>.API/Grpc/<Service>GrpcService.cs`.
3. На стороне клиента: подключите `<Protobuf Include="...proto" GrpcServices="Client"/>` в csproj сервиса-потребителя.
4. Зарегистрируйте `AddGrpcClient<...>` в его `DependencyInjection.cs`.
5. Если у сервиса-сервера ещё нет отдельного порта gRPC — в `Program.cs` через `ConfigureKestrel` поднимите `8081` с `HttpProtocols.Http2`. Обновите `Services__<Name>__GrpcUrl` в `docker-compose.yml` для всех потребителей.
6. У потребителя в `Program.cs` обязательно `AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true)` — без этого .NET-клиент не пойдёт по plaintext HTTP/2.

### Соглашения по коду

- Nullable везде включён (`<Nullable>enable</Nullable>`) — non-nullable reference types автоматически попадают в `required` Swagger-схемы.
- Implicit usings включены.
- `System.Text.Json` для всей сериализации (без Newtonsoft.Json).
- Async/await везде — никаких блокирующих вызовов (`.Result`, `.Wait()`).
- Доменная логика только в слое Domain; никакой бизнес-логики в контроллерах или репозиториях.
- Каждое изменение состояния агрегата должно поднимать domain event.
- Контроллеры не должны напрямую внедрять репозитории — все чтения/записи проходят через MediatR.

---

## Структура проекта

```
ResX/
├── src/
│   ├── BuildingBlocks/
│   │   ├── ResX.Common/               # Базовые: AggregateRoot, Entity, ValueObject, исключения, Swagger-extension
│   │   ├── ResX.EventBus.RabbitMQ/    # Реализация IEventBus через RabbitMQ
│   │   ├── ResX.Caching.Redis/        # Реализация ICacheService через Redis
│   │   └── ResX.Storage.S3/           # S3/MinIO storage client (с поддержкой PublicUrl override)
│   ├── Protos/
│   │   ├── identity.proto
│   │   ├── users.proto
│   │   ├── listings.proto
│   │   └── files.proto
│   ├── Services/
│   │   ├── Analytics/
│   │   │   ├── ResX.Analytics.Application/
│   │   │   ├── ResX.Analytics.Infrastructure/
│   │   │   └── ResX.Analytics.API/
│   │   ├── Charity/        (аналогичная 3-слойная структура)
│   │   ├── Disputes/       (аналогичная 3-слойная структура)
│   │   ├── Files/          (аналогичная 3-слойная структура)
│   │   ├── Identity/       (аналогичная 3-слойная структура)
│   │   ├── Listings/       (аналогичная 3-слойная структура)
│   │   ├── Messaging/      (аналогичная 3-слойная структура)
│   │   ├── Notifications/  (аналогичная 3-слойная структура)
│   │   ├── Transactions/   (аналогичная 3-слойная структура)
│   │   └── Users/          (аналогичная 3-слойная структура)
│   └── Gateway/
│       └── ResX.ApiGateway/           # YARP reverse proxy gateway
├── tests/
│   ├── ResX.IntegrationTests.Common/  # PostgresContainerFixture, JwtTokenHelper, HttpClientExtensions
│   ├── ResX.Analytics.IntegrationTests/
│   ├── ResX.Charity.IntegrationTests/
│   ├── ResX.Disputes.IntegrationTests/
│   ├── ResX.Files.IntegrationTests/
│   ├── ResX.Identity.IntegrationTests/
│   ├── ResX.Listings.IntegrationTests/
│   ├── ResX.Messaging.IntegrationTests/
│   ├── ResX.Notifications.IntegrationTests/
│   ├── ResX.Transactions.IntegrationTests/
│   └── ResX.Users.IntegrationTests/
├── scripts/
│   └── init-databases.sql             # Создаёт все БД PostgreSQL при первом запуске
├── docker-compose.yml                 # Cloud-ready compose для Timeweb App Platform
├── docker-compose.local.yml           # Локальная разработка: gateway + 10 сервисов + Postgres/Redis/RabbitMQ/MinIO
├── .env.example                       # Шаблон env-переменных для Timeweb-деплоя (копируй в .env)
└── ResX.sln                           # Visual Studio solution
```

---

## Участие в разработке

1. **Сделайте fork** репозитория и создайте feature-ветку:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Следуйте Clean Architecture**: бизнес-логика — в слоях Domain или Application, но не в Infrastructure или API.

3. **Пишите тесты**: интеграционные тесты для репозиториев и API endpoint'ов с использованием Testcontainers.

4. **Миграции только аддитивные**: никогда не редактируйте существующие классы миграций FluentMigrator — всегда создавайте новые.

5. **Откройте Pull Request** в `main` с понятным описанием и номерами связанных задач.

### Именование веток

| Тип           | Шаблон                      |
|---------------|-----------------------------|
| Функция       | `feature/short-description` |
| Исправление   | `fix/short-description`     |
| Рефакторинг   | `refactor/short-description`|
| Инфраструктура| `infra/short-description`   |

---

## Лицензия

Данное программное обеспечение является проприетарным. Все права защищены.

---

*Создано на .NET 10 · PostgreSQL · RabbitMQ · Redis · MinIO · Docker Compose*
