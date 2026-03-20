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
- [Развёртывание в Kubernetes](#развёртывание-в-kubernetes)
- [Переменные окружения](#переменные-окружения)
- [API Endpoints](#api-endpoints)
- [Доменные модели](#доменные-модели)
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
- Обмен сообщениями в реальном времени (SignalR WebSocket)
- Благотворительные кампании НКО с процессом пожертвования вещей
- Разрешение споров по несостоявшимся транзакциям
- Загрузка файлов (фото) в S3-совместимое объектное хранилище
- Аналитика: эко-влияние, тренды по категориям, активность по городам
- Полноценная JWT-аутентификация с ротацией refresh-токенов и Redis-блэклистом

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
- **HTTP/REST** — клиент ↔ API Gateway ↔ сервисы
- **gRPC** — синхронные межсервисные вызовы (Identity→Users, Listings→Files и т.д.)
- **RabbitMQ** — асинхронные интеграционные события (UserRegistered, ListingCreated, TransactionCompleted, MessageSent и др.)
- **SignalR** — real-time WebSocket для обмена сообщениями

**Архитектура каждого сервиса:** Clean Architecture + DDD
```
ResX.<Service>.Domain          ← Aggregates, Value Objects, Domain Events
ResX.<Service>.Application     ← Commands, Queries (CQRS/MediatR), DTOs, интерфейсы репозиториев
ResX.<Service>.Infrastructure  ← EF Core DbContext, реализации репозиториев, FluentMigrator
ResX.<Service>.API             ← ASP.NET Core Controllers, Program.cs
```

---

## Сервисы

| Сервис               | Внутренний порт | Dev-порт | Описание                                          |
|----------------------|:---------------:|:--------:|---------------------------------------------------|
| **API Gateway**      | 8080            | 8080     | YARP reverse proxy, JWT-аутентификация, rate limit|
| **Identity Service** | 8080            | 5001     | Регистрация, вход, выдача JWT, refresh-токены     |
| **Users Service**    | 8080            | 5002     | Профили, репутация, отзывы, эко-статистика        |
| **Listings Service** | 8080            | 5003     | Объявления, поиск, категории, status machine      |
| **Transactions Svc** | 8080            | 5004     | Жизненный цикл транзакций, подтверждения, споры   |
| **Messaging Service**| 8080            | 5005     | Real-time чат через SignalR, история сообщений    |
| **Notifications Svc**| 8080            | 5006     | Внутренние уведомления через consumption событий  |
| **Charity Service**  | 8080            | 5007     | НКО-организации, благотворительные заявки         |
| **Disputes Service** | 8080            | 5008     | Споры, доказательства, решение модератора         |
| **Files Service**    | 8080            | 5009     | Загрузка/скачивание файлов через S3, pre-signed URLs|
| **Analytics Service**| 8080            | 5010     | Эко-статистика, тренды по категориям, активность по городам|

**Инфраструктура:**

| Компонент    | Dev-порт | Назначение                                  |
|--------------|:--------:|---------------------------------------------|
| PostgreSQL   | 5432     | Основное хранилище данных (отдельная БД на сервис)|
| RabbitMQ     | 5672     | Message broker                              |
| RabbitMQ UI  | 15672    | Management console                          |
| Redis        | 6379     | Распределённый кэш и блэклист токенов       |
| MinIO        | 9000     | S3-совместимое объектное хранилище (локально)|
| MinIO UI     | 9001     | Web-консоль MinIO                           |

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
| Real-time         | SignalR (WebSocket, Long Polling fallback)           |
| RPC               | gRPC (Google.Protobuf, Grpc.AspNetCore)             |
| Кэширование       | Redis (StackExchange.Redis)                         |
| Объектное хранилище| S3 (AWSSDK.S3 — Yandex Object Storage / MinIO)    |
| Аутентификация    | JWT Bearer (Microsoft.AspNetCore.Authentication)    |
| API-документация  | Swagger / OpenAPI (Swashbuckle)                     |
| Health Checks     | ASP.NET Core HealthChecks + Npgsql                  |
| Контейнеризация   | Docker (multi-stage builds)                         |
| Оркестрация       | Kubernetes (Deployments, StatefulSets, HPA, Ingress)|
| База данных       | PostgreSQL 16                                       |
| Тестирование      | xUnit, FluentAssertions, Testcontainers, NSubstitute|

---

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (с Docker Compose v2)
- [kubectl](https://kubernetes.io/docs/tasks/tools/) (для развёртывания в Kubernetes)
- Опционально: [k9s](https://k9scli.io/), [Lens](https://k8slens.dev/) для мониторинга кластера

---

## Быстрый старт (Docker Compose)

### 1. Клонирование репозитория

```bash
git clone https://github.com/your-org/resx.git
cd resx
```

### 2. Настройка окружения

Скопируйте и отредактируйте файл переменных окружения:

```bash
cp .env.example .env
```

Минимально необходимые значения в `.env`:

```env
POSTGRES_PASSWORD=your_postgres_password
RABBITMQ_PASSWORD=your_rabbitmq_password
REDIS_PASSWORD=your_redis_password
JWT_SECRET_KEY=your_super_secret_jwt_key_min_32_chars
S3_ACCESS_KEY=minioadmin
S3_SECRET_KEY=minioadmin
```

### 3. Запуск всех сервисов

```bash
docker compose up --build
```

Или сначала только инфраструктура, затем сервисы:

```bash
docker compose up postgres rabbitmq redis minio
docker compose up --build identity-service users-service listings-service api-gateway
```

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
docker compose down
# Удалить также volumes (все данные будут удалены):
docker compose down -v
```

---

## Развёртывание в Kubernetes

### Требования

- Работающий кластер Kubernetes (1.28+)
- `kubectl`, настроенный на этот кластер
- Установленный NGINX Ingress Controller
- Установленный cert-manager (для TLS)

### 1. Создание namespace

```bash
kubectl apply -f k8s/namespace.yaml
```

### 2. Создание секретов

Скопируйте шаблон и заполните реальными значениями:

```bash
cp k8s/secrets.yaml.template k8s/secrets.yaml
# Отредактируйте k8s/secrets.yaml — замените все плейсхолдеры <base64-encoded-*>
# Генерация base64: echo -n 'value' | base64
kubectl apply -f k8s/secrets.yaml
```

> **Никогда не коммитьте `k8s/secrets.yaml` в систему контроля версий.** Файл добавлен в `.gitignore`.

### 3. Применение ConfigMap

```bash
kubectl apply -f k8s/configmap.yaml
```

### 4. Развёртывание инфраструктуры

```bash
kubectl apply -f k8s/infrastructure/
```

Ожидание готовности инфраструктуры:

```bash
kubectl -n resx rollout status statefulset/postgres
kubectl -n resx rollout status statefulset/rabbitmq
kubectl -n resx rollout status deployment/redis
```

### 5. Развёртывание сервисов

```bash
kubectl apply -f k8s/services/
kubectl apply -f k8s/ingress.yaml
```

### 6. Проверка rollout

```bash
kubectl -n resx get pods
kubectl -n resx get ingress
```

### 7. Настройка DNS

Направьте `api.resx.ru` на внешний IP Ingress-контроллера кластера:

```bash
kubectl -n resx get ingress resx-ingress
# Скопируйте ADDRESS и создайте A-запись у вашего DNS-провайдера
```

### Масштабирование

HPA настроены для всех сервисов. Ручное управление:

```bash
kubectl -n resx scale deployment identity-service --replicas=3
```

---

## Переменные окружения

### Общие (все сервисы)

| Переменная                             | Описание                               | По умолчанию (dev)          |
|----------------------------------------|----------------------------------------|-----------------------------|
| `ASPNETCORE_ENVIRONMENT`               | Среда выполнения                       | `Development`               |
| `ConnectionStrings__DefaultConnection` | Строка подключения к PostgreSQL        | `Host=postgres;Database=...`|
| `Jwt__SecretKey`                       | Ключ подписи JWT (минимум 32 символа)  | —                           |
| `Jwt__Issuer`                          | Издатель JWT                           | `ResX`                      |
| `Jwt__Audience`                        | Аудитория JWT                          | `ResX`                      |
| `Jwt__ExpiryMinutes`                   | Время жизни access-токена (минуты)     | `60`                        |
| `RabbitMQ__HostName`                   | Хост брокера RabbitMQ                  | `rabbitmq`                  |
| `RabbitMQ__UserName`                   | Имя пользователя RabbitMQ              | `guest`                     |
| `RabbitMQ__Password`                   | Пароль RabbitMQ                        | —                           |
| `Redis__ConnectionString`              | Строка подключения к Redis             | `redis:6379,password=...`   |
| `Logging__LogLevel__Default`           | Уровень логирования                    | `Information`               |

### Только Files Service

| Переменная       | Описание                              | По умолчанию (dev)     |
|------------------|---------------------------------------|------------------------|
| `S3__ServiceUrl` | URL S3-эндпоинта                      | `http://minio:9000`    |
| `S3__AccessKey`  | Access key S3                         | `minioadmin`           |
| `S3__SecretKey`  | Secret key S3                         | —                      |
| `S3__BucketName` | Имя bucket S3                         | `resx-files`           |
| `S3__Region`     | Регион AWS (Yandex: `ru-central1`)    | `us-east-1`            |

### Только Analytics Service

| Переменная                           | Описание                                  |
|--------------------------------------|-------------------------------------------|
| `ConnectionStrings__UsersDb`         | Read-подключение к БД Users               |
| `ConnectionStrings__ListingsDb`      | Read-подключение к БД Listings            |
| `ConnectionStrings__TransactionsDb`  | Read-подключение к БД Transactions        |

---

## API Endpoints

Все endpoint'ы доступны через API Gateway: `https://api.resx.ru` (prod) или `http://localhost:8080` (dev).

### Identity Service `/api/auth`

| Метод | Путь                        | Auth | Описание                         |
|-------|-----------------------------|------|----------------------------------|
| POST  | `/api/auth/register`        | —    | Регистрация нового пользователя  |
| POST  | `/api/auth/login`           | —    | Вход, возвращает JWT + refresh   |
| POST  | `/api/auth/refresh`         | —    | Обновление access-токена         |
| POST  | `/api/auth/logout`          | JWT  | Отзыв refresh-токена             |
| POST  | `/api/auth/confirm-email`   | —    | Подтверждение email по токену    |
| POST  | `/api/auth/change-password` | JWT  | Смена пароля                     |

### Users Service `/api/users`

| Метод | Путь                             | Auth  | Описание                            |
|-------|----------------------------------|-------|-------------------------------------|
| GET   | `/api/users/{id}`                | —     | Получить профиль пользователя       |
| PUT   | `/api/users/me`                  | JWT   | Обновить собственный профиль        |
| PUT   | `/api/users/me/avatar`           | JWT   | Обновить аватар                     |
| GET   | `/api/users/{id}/reviews`        | —     | Получить отзывы о пользователе      |
| POST  | `/api/users/{id}/reviews`        | JWT   | Оставить отзыв о пользователе       |
| GET   | `/api/users/eco-leaderboard`     | —     | Эко-рейтинг пользователей           |

### Listings Service `/api/listings`, `/api/categories`

| Метод  | Путь                            | Auth | Описание                             |
|--------|---------------------------------|------|--------------------------------------|
| GET    | `/api/listings`                 | —    | Поиск/фильтрация объявлений (кэш)    |
| POST   | `/api/listings`                 | JWT  | Создать объявление (статус Draft)    |
| GET    | `/api/listings/{id}`            | —    | Получить детали объявления           |
| PUT    | `/api/listings/{id}`            | JWT  | Обновить своё объявление             |
| DELETE | `/api/listings/{id}`            | JWT  | Удалить своё объявление              |
| PATCH  | `/api/listings/{id}/status`     | JWT  | Изменить статус объявления           |
| POST   | `/api/listings/{id}/photos`     | JWT  | Добавить фото к объявлению           |
| GET    | `/api/listings/my`              | JWT  | Получить объявления текущего пользователя|
| GET    | `/api/categories`               | —    | Список всех категорий                |

### Transactions Service `/api/transactions`

| Метод | Путь                                     | Auth | Описание                                      |
|-------|------------------------------------------|------|-----------------------------------------------|
| GET   | `/api/transactions`                      | JWT  | Мои транзакции                                |
| POST  | `/api/transactions`                      | JWT  | Создать транзакцию (вызывает получатель)      |
| GET   | `/api/transactions/{id}`                 | JWT  | Получить детали транзакции                    |
| POST  | `/api/transactions/{id}/agree`           | JWT  | Донор соглашается (Pending → DonorAgreed)     |
| POST  | `/api/transactions/{id}/confirm-receipt` | JWT  | Получатель подтверждает (DonorAgreed → Completed)|
| POST  | `/api/transactions/{id}/cancel`          | JWT  | Отменить транзакцию (любой участник)          |
| POST  | `/api/transactions/{id}/dispute`         | JWT  | Открыть спор по транзакции                    |

### Messaging Service `/api/messaging`

| Метод     | Путь                                          | Auth | Описание                       |
|-----------|-----------------------------------------------|------|--------------------------------|
| GET       | `/api/messaging/conversations`                | JWT  | Список диалогов пользователя   |
| GET       | `/api/messaging/conversations/{id}/messages`  | JWT  | Сообщения диалога              |
| POST      | `/api/messaging/conversations`                | JWT  | Начать новый диалог            |
| POST      | `/api/messaging/conversations/{id}/messages`  | JWT  | Отправить сообщение            |
| POST      | `/api/messaging/conversations/{id}/read`      | JWT  | Отметить сообщения прочитанными|
| WebSocket | `/hubs/messaging`                             | JWT  | Real-time SignalR hub          |

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
| POST  | `/api/charity/requests`                       | JWT   | Создать заявку (только верифицированная НКО)|
| POST  | `/api/charity/requests/{id}/cancel`           | Admin | Отменить благотворительную заявку          |
| POST  | `/api/charity/requests/{id}/complete`         | Admin | Завершить благотворительную заявку         |
| GET   | `/api/charity/organizations/{id}`             | —     | Получить данные организации                |
| POST  | `/api/charity/organizations`                  | JWT   | Зарегистрировать новую организацию         |
| PUT   | `/api/charity/organizations/{id}/verify`      | Admin | Верифицировать организацию                 |
| PUT   | `/api/charity/organizations/{id}/reject`      | Admin | Отклонить верификацию организации          |

### Disputes Service `/api/disputes`

| Метод | Путь                            | Auth       | Описание                              |
|-------|---------------------------------|------------|---------------------------------------|
| GET   | `/api/disputes`                 | JWT        | Мои споры                             |
| POST  | `/api/disputes`                 | JWT        | Открыть новый спор                    |
| GET   | `/api/disputes/{id}`            | JWT        | Детали спора с доказательствами       |
| POST  | `/api/disputes/{id}/evidence`   | JWT        | Добавить доказательство к спору       |
| POST  | `/api/disputes/{id}/resolve`    | Moderator  | Разрешить спор с выводом              |
| POST  | `/api/disputes/{id}/close`      | Moderator  | Закрыть спор без решения              |
| GET   | `/api/disputes/open`            | Moderator  | Список открытых/рассматриваемых споров|

### Files Service `/api/files`

| Метод  | Путь                   | Auth | Описание                              |
|--------|------------------------|------|---------------------------------------|
| POST   | `/api/files/upload`    | JWT  | Загрузить файл (multipart/form-data)  |
| GET    | `/api/files/{id}/url`  | JWT  | Получить pre-signed URL для скачивания|
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
| Draft → Cancelled       | Владелец (удаление черновика)|
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

| Переход                     | Инициатор                      |
|-----------------------------|--------------------------------|
| Pending → DonorAgreed       | Донор (`/agree`)               |
| DonorAgreed → Completed     | Получатель (`/confirm-receipt`)|
| Pending → Cancelled         | Любой участник                 |
| DonorAgreed → Cancelled     | Любой участник                 |
| Pending → Disputed          | Любой участник                 |
| DonorAgreed → Disputed      | Любой участник                 |

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

Только **верифицированные** организации могут создавать благотворительные заявки.

---

## Стратегия кэширования

Redis используется в трёх сервисах с разными подходами:

### Listings — version-based cache invalidation

Все ответы `GET /api/listings` кэшируются на **5 минут**. В Redis хранится монотонный счётчик версий (`listings:version`), который инкрементируется при любом изменении (создание, обновление, удаление, смена статуса). Ключ кэша содержит версию, поэтому устаревшие записи обходятся автоматически — без явного удаления.

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

Для каждого сервиса есть отдельный проект интеграционных тестов, использующий реальный экземпляр PostgreSQL через **Testcontainers**. База данных сбрасывается между тестами с помощью **Respawner**.

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
dotnet test tests/ResX.Disputes.IntegrationTests/
```

> Тесты автоматически поднимают контейнер PostgreSQL — ручная настройка не нужна.

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

1. Определите класс события в папке `Application/IntegrationEvents/` публикующего сервиса
2. Опубликуйте через `IEventBus.PublishAsync()` в обработчике команды
3. Создайте обработчик в потребляющем сервисе, реализующий `IIntegrationEventHandler<TEvent>`
4. Зарегистрируйте обработчик в `DependencyInjection.cs` слоя Infrastructure потребляющего сервиса

### Добавление нового gRPC endpoint

1. Добавьте метод в соответствующий `.proto`-файл в `src/Protos/`
2. Пересоберите решение (генерация кода из Protobuf запускается автоматически)
3. Реализуйте серверный метод сервиса в Infrastructure
4. Внедрите сгенерированный gRPC-клиент в потребляющих сервисах

### Соглашения по коду

- Nullable везде включён (`<Nullable>enable</Nullable>`)
- Implicit usings включены
- `System.Text.Json` для всей сериализации (без Newtonsoft.Json)
- Async/await везде — никаких блокирующих вызовов (`.Result`, `.Wait()`)
- Доменная логика только в слое Domain; никакой бизнес-логики в контроллерах или репозиториях
- Каждое изменение состояния агрегата должно поднимать domain event
- Контроллеры не должны напрямую внедрять репозитории — все чтения/записи проходят через MediatR

---

## Структура проекта

```
ResX/
├── src/
│   ├── BuildingBlocks/
│   │   ├── ResX.Common/               # Базовые классы: AggregateRoot, Entity, ValueObject, исключения
│   │   ├── ResX.EventBus.RabbitMQ/    # Реализация IEventBus через RabbitMQ
│   │   ├── ResX.Caching.Redis/        # Реализация ICacheService через Redis
│   │   └── ResX.Storage.S3/           # S3/MinIO storage client
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
│   ├── ResX.IntegrationTests.Common/  # Общее: PostgresContainerFixture, JwtTokenHelper, HttpClientExtensions
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
├── k8s/
│   ├── namespace.yaml
│   ├── configmap.yaml
│   ├── secrets.yaml.template
│   ├── ingress.yaml
│   ├── infrastructure/
│   │   ├── postgres.yaml
│   │   ├── rabbitmq.yaml
│   │   ├── redis.yaml
│   │   └── minio.yaml
│   └── services/
│       ├── identity-service.yaml
│       ├── microservices.yaml
│       └── api-gateway.yaml
├── scripts/
│   └── init-databases.sql             # Создаёт все БД PostgreSQL при первом запуске
├── docker-compose.yml                 # Полная оркестрация локального окружения
├── docker-compose.override.yml        # Dev-переопределения (debug-логирование, hot-reload)
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

*Создано на .NET 10 · PostgreSQL · RabbitMQ · Redis · Kubernetes*
