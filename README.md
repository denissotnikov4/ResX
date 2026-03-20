# ResX — Resource Crossing Platform

> A P2P platform for reducing municipal solid waste by enabling gifting, exchange, and charitable donation of still-usable items between citizens and NGOs.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Services](#services)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick Start (Docker Compose)](#quick-start-docker-compose)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Environment Variables](#environment-variables)
- [API Endpoints](#api-endpoints)
- [Development Workflow](#development-workflow)
- [Project Structure](#project-structure)
- [Contributing](#contributing)

---

## Overview

**ResX** (ресурс-кроссинг) is a backend platform that connects people who have items they no longer need with those who can use them — reducing landfill waste and fostering circular economy within cities.

**Key capabilities:**
- Post listings for free gifting, barter exchange, or charitable donation
- Real-time messaging between users (SignalR WebSocket)
- NGO charity campaigns with item donation workflows
- Dispute resolution for failed transactions
- File uploads (photos) stored in S3-compatible object storage
- Analytics on eco-impact, category trends, and city activity
- Full JWT-based authentication and authorization

---

## Architecture

```
                           +-------------------------------------+
                           |            Client Apps              |
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

                    +-------------- Infrastructure ----------------+
                    |  PostgreSQL . RabbitMQ . Redis . MinIO/S3   |
                    +---------------------------------------------+
```

**Communication patterns:**
- **HTTP/REST** — client <-> API Gateway <-> services
- **gRPC** — synchronous inter-service calls (Identity->Users, Listings->Files, etc.)
- **RabbitMQ** — async integration events (UserRegistered, ListingPublished, OrderCreated, etc.)
- **SignalR** — real-time WebSocket for messaging

**Architecture per service:** Clean Architecture + DDD
```
ResX.<Service>.Domain          <- Aggregates, Value Objects, Domain Events, Repository interfaces
ResX.<Service>.Application     <- Commands, Queries (CQRS/MediatR), DTOs, Service interfaces
ResX.<Service>.Infrastructure  <- EF Core DbContext, Repository implementations, FluentMigrator, gRPC clients
ResX.<Service>.API             <- ASP.NET Core Controllers, Middleware, Program.cs
```

---

## Services

| Service              | Internal Port | Dev Port | Description                                      |
|----------------------|:-------------:|:--------:|--------------------------------------------------|
| **API Gateway**      | 8080          | 8080     | YARP reverse proxy, JWT auth, rate limiting      |
| **Identity Service** | 8080          | 5001     | Registration, login, JWT issuance, refresh tokens|
| **Users Service**    | 8080          | 5002     | User profiles, reputation, ratings               |
| **Listings Service** | 8080          | 5003     | Item listings, search, categories, moderation    |
| **Transactions Svc** | 8080          | 5004     | Requests, confirmations, exchange workflows      |
| **Messaging Service**| 8080          | 5005     | Real-time chat via SignalR, message history      |
| **Notifications Svc**| 8080          | 5006     | Push/email notifications via event consumption   |
| **Charity Service**  | 8080          | 5007     | NGO campaigns, donation management               |
| **Disputes Service** | 8080          | 5008     | Dispute filing, evidence, moderator resolution   |
| **Files Service**    | 8080          | 5009     | S3 file upload/download, image processing        |
| **Analytics Service**| 8080          | 5010     | Eco stats, category trends, city activity        |

**Infrastructure:**

| Component    | Dev Port | Purpose                              |
|--------------|:--------:|--------------------------------------|
| PostgreSQL   | 5432     | Primary data store (9 databases)     |
| RabbitMQ     | 5672     | Message broker                       |
| RabbitMQ UI  | 15672    | Management console                   |
| Redis        | 6379     | Distributed cache & sessions         |
| MinIO        | 9000     | S3-compatible object storage (local) |
| MinIO UI     | 9001     | MinIO web console                    |

---

## Tech Stack

| Layer             | Technology                                          |
|-------------------|-----------------------------------------------------|
| Language          | C# 13 / .NET 10                                     |
| Web Framework     | ASP.NET Core 10                                     |
| API Gateway       | YARP (Yet Another Reverse Proxy) 2.3                |
| ORM               | Entity Framework Core 10 (Npgsql provider)          |
| Migrations        | FluentMigrator                                      |
| CQRS/Mediator     | MediatR 12                                          |
| Validation        | FluentValidation                                    |
| Serialization     | System.Text.Json (camelCase)                        |
| Messaging         | RabbitMQ + custom EventBus building block           |
| Real-time         | SignalR (WebSocket, Long Polling fallback)           |
| RPC               | gRPC (Google.Protobuf, Grpc.AspNetCore)             |
| Caching           | Redis (StackExchange.Redis)                         |
| Object Storage    | S3 (AWSSDK.S3 — Yandex Object Storage / MinIO)     |
| Authentication    | JWT Bearer (Microsoft.AspNetCore.Authentication)    |
| API Docs          | Swagger / OpenAPI (Swashbuckle)                     |
| Health Checks     | ASP.NET Core HealthChecks + Npgsql                  |
| Containerization  | Docker (multi-stage builds)                         |
| Orchestration     | Kubernetes (Deployments, StatefulSets, HPA, Ingress)|
| Database          | PostgreSQL 16                                       |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (with Docker Compose v2)
- [kubectl](https://kubernetes.io/docs/tasks/tools/) (for Kubernetes deployment)
- Optionally: [k9s](https://k9scli.io/), [Lens](https://k8slens.dev/) for cluster monitoring

---

## Quick Start (Docker Compose)

### 1. Clone the repository

```bash
git clone https://github.com/your-org/resx.git
cd resx
```

### 2. Configure environment

Copy and edit the environment file:

```bash
cp .env.example .env
```

Minimum required values in `.env`:

```env
POSTGRES_PASSWORD=your_postgres_password
RABBITMQ_PASSWORD=your_rabbitmq_password
REDIS_PASSWORD=your_redis_password
JWT_SECRET_KEY=your_super_secret_jwt_key_min_32_chars
S3_ACCESS_KEY=minioadmin
S3_SECRET_KEY=minioadmin
```

### 3. Start all services

```bash
docker compose up --build
```

Or run only infrastructure first, then services:

```bash
docker compose up postgres rabbitmq redis minio
docker compose up --build identity-service users-service listings-service api-gateway
```

### 4. Verify

| URL                                    | Description                    |
|----------------------------------------|--------------------------------|
| http://localhost:8080/health           | API Gateway health check       |
| http://localhost:5001/swagger          | Identity Service Swagger UI    |
| http://localhost:5002/swagger          | Users Service Swagger UI       |
| http://localhost:5003/swagger          | Listings Service Swagger UI    |
| http://localhost:5004/swagger          | Transactions Service Swagger   |
| http://localhost:5005/swagger          | Messaging Service Swagger      |
| http://localhost:5006/swagger          | Notifications Service Swagger  |
| http://localhost:5007/swagger          | Charity Service Swagger        |
| http://localhost:5008/swagger          | Disputes Service Swagger       |
| http://localhost:5009/swagger          | Files Service Swagger          |
| http://localhost:5010/swagger          | Analytics Service Swagger      |
| http://localhost:15672                 | RabbitMQ Management UI         |
| http://localhost:9001                  | MinIO Console                  |

### 5. Stop

```bash
docker compose down
# To also remove volumes (deletes all data):
docker compose down -v
```

---

## Kubernetes Deployment

### Prerequisites

- A running Kubernetes cluster (1.28+)
- `kubectl` configured to point at the cluster
- NGINX Ingress Controller installed
- cert-manager installed (for TLS)

### 1. Create namespace

```bash
kubectl apply -f k8s/namespace.yaml
```

### 2. Create secrets

Copy the template and fill in real values:

```bash
cp k8s/secrets.yaml.template k8s/secrets.yaml
# Edit k8s/secrets.yaml — replace all <base64-encoded-*> placeholders
# Generate base64 values: echo -n 'value' | base64
kubectl apply -f k8s/secrets.yaml
```

> **Never commit `k8s/secrets.yaml` to version control.** The file is gitignored.

### 3. Apply ConfigMap

```bash
kubectl apply -f k8s/configmap.yaml
```

### 4. Deploy infrastructure

```bash
kubectl apply -f k8s/infrastructure/
```

Wait for infrastructure to be ready:

```bash
kubectl -n resx rollout status statefulset/postgres
kubectl -n resx rollout status statefulset/rabbitmq
kubectl -n resx rollout status deployment/redis
```

### 5. Deploy services

```bash
kubectl apply -f k8s/services/
kubectl apply -f k8s/ingress.yaml
```

### 6. Check rollout

```bash
kubectl -n resx get pods
kubectl -n resx get ingress
```

### 7. DNS configuration

Point `api.resx.ru` to your cluster's Ingress external IP:

```bash
kubectl -n resx get ingress resx-ingress
# Copy the ADDRESS and create an A record in your DNS provider
```

### Scaling

HPAs are configured for all services. Manual override:

```bash
kubectl -n resx scale deployment identity-service --replicas=3
```

---

## Environment Variables

### Shared (all services)

| Variable                               | Description                          | Default (dev)              |
|----------------------------------------|--------------------------------------|----------------------------|
| `ASPNETCORE_ENVIRONMENT`               | Runtime environment                  | `Development`              |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string         | `Host=postgres;Database=...`|
| `Jwt__SecretKey`                       | JWT signing key (min 32 chars)       | —                          |
| `Jwt__Issuer`                          | JWT issuer                           | `ResX`                     |
| `Jwt__Audience`                        | JWT audience                         | `ResX`                     |
| `Jwt__ExpiryMinutes`                   | Access token lifetime (minutes)      | `60`                       |
| `RabbitMQ__HostName`                   | RabbitMQ broker host                 | `rabbitmq`                 |
| `RabbitMQ__UserName`                   | RabbitMQ username                    | `guest`                    |
| `RabbitMQ__Password`                   | RabbitMQ password                    | —                          |
| `Redis__ConnectionString`              | Redis connection string              | `redis:6379,password=...`  |
| `Logging__LogLevel__Default`           | Log level                            | `Information`              |

### Files Service only

| Variable         | Description                         | Default (dev)          |
|------------------|-------------------------------------|------------------------|
| `S3__ServiceUrl` | S3 endpoint URL                     | `http://minio:9000`    |
| `S3__AccessKey`  | S3 access key                       | `minioadmin`           |
| `S3__SecretKey`  | S3 secret key                       | —                      |
| `S3__BucketName` | S3 bucket name                      | `resx-files`           |
| `S3__Region`     | AWS region (Yandex: `ru-central1`)  | `us-east-1`            |

### Analytics Service only

| Variable                             | Description                           |
|--------------------------------------|---------------------------------------|
| `ConnectionStrings__UsersDb`         | Read connection to Users DB           |
| `ConnectionStrings__ListingsDb`      | Read connection to Listings DB        |
| `ConnectionStrings__TransactionsDb`  | Read connection to Transactions DB    |

---

## API Endpoints

All endpoints are accessible via the API Gateway at `https://api.resx.ru` (prod) or `http://localhost:8080` (dev).

### Identity Service `/api/auth`

| Method | Path                        | Auth | Description                     |
|--------|-----------------------------|------|---------------------------------|
| POST   | `/api/auth/register`        | —    | Register new user               |
| POST   | `/api/auth/login`           | —    | Login, returns JWT + refresh    |
| POST   | `/api/auth/refresh`         | —    | Refresh access token            |
| POST   | `/api/auth/logout`          | JWT  | Revoke refresh token            |
| POST   | `/api/auth/confirm-email`   | —    | Confirm email with token        |

### Users Service `/api/users`

| Method | Path                      | Auth | Description                     |
|--------|---------------------------|------|---------------------------------|
| GET    | `/api/users/{id}`         | —    | Get user profile                |
| PUT    | `/api/users/me`           | JWT  | Update own profile              |
| GET    | `/api/users/{id}/ratings` | —    | Get user ratings                |
| POST   | `/api/users/{id}/rate`    | JWT  | Submit rating for user          |

### Listings Service `/api/listings`

| Method | Path                          | Auth | Description                   |
|--------|-------------------------------|------|-------------------------------|
| GET    | `/api/listings`               | —    | Search/filter listings        |
| POST   | `/api/listings`               | JWT  | Create listing                |
| GET    | `/api/listings/{id}`          | —    | Get listing details           |
| PUT    | `/api/listings/{id}`          | JWT  | Update own listing            |
| DELETE | `/api/listings/{id}`          | JWT  | Delete own listing            |
| GET    | `/api/listings/categories`    | —    | List all categories           |
| GET    | `/api/listings/my`            | JWT  | Get current user's listings   |

### Transactions Service `/api/transactions`

| Method | Path                                | Auth | Description                    |
|--------|-------------------------------------|------|--------------------------------|
| POST   | `/api/transactions`                 | JWT  | Create transaction request     |
| GET    | `/api/transactions/{id}`            | JWT  | Get transaction details        |
| POST   | `/api/transactions/{id}/confirm`    | JWT  | Confirm receipt                |
| POST   | `/api/transactions/{id}/cancel`     | JWT  | Cancel transaction             |
| GET    | `/api/transactions/my`              | JWT  | Get my transactions            |

### Messaging Service `/api/messages`

| Method    | Path                              | Auth | Description                   |
|-----------|-----------------------------------|------|-------------------------------|
| GET       | `/api/messages/chats`             | JWT  | List user's chats             |
| GET       | `/api/messages/chats/{id}`        | JWT  | Get chat messages             |
| POST      | `/api/messages/chats`             | JWT  | Start new chat                |
| POST      | `/api/messages/chats/{id}/send`   | JWT  | Send message                  |
| WebSocket | `/hubs/messaging`                 | JWT  | Real-time SignalR hub         |

### Notifications Service `/api/notifications`

| Method | Path                              | Auth | Description                |
|--------|-----------------------------------|------|----------------------------|
| GET    | `/api/notifications`              | JWT  | Get notifications          |
| PUT    | `/api/notifications/{id}/read`    | JWT  | Mark as read               |
| PUT    | `/api/notifications/read-all`     | JWT  | Mark all as read           |
| DELETE | `/api/notifications/{id}`         | JWT  | Delete notification        |

### Charity Service `/api/charity`

| Method | Path                                  | Auth | Description                    |
|--------|---------------------------------------|------|--------------------------------|
| GET    | `/api/charity/campaigns`              | —    | List active campaigns          |
| POST   | `/api/charity/campaigns`              | JWT  | Create campaign (NGO only)     |
| GET    | `/api/charity/campaigns/{id}`         | —    | Get campaign details           |
| POST   | `/api/charity/campaigns/{id}/donate`  | JWT  | Donate item to campaign        |

### Disputes Service `/api/disputes`

| Method | Path                            | Auth | Description                   |
|--------|---------------------------------|------|-------------------------------|
| POST   | `/api/disputes`                 | JWT  | File new dispute              |
| GET    | `/api/disputes/{id}`            | JWT  | Get dispute details           |
| POST   | `/api/disputes/{id}/evidence`   | JWT  | Upload evidence               |
| PUT    | `/api/disputes/{id}/resolve`    | JWT  | Resolve dispute (moderator)   |

### Files Service `/api/files`

| Method | Path                   | Auth | Description                       |
|--------|------------------------|------|-----------------------------------|
| POST   | `/api/files/upload`    | JWT  | Upload file (multipart/form-data) |
| GET    | `/api/files/{id}`      | —    | Get file metadata                 |
| GET    | `/api/files/{id}/url`  | —    | Get pre-signed download URL       |
| DELETE | `/api/files/{id}`      | JWT  | Delete own file                   |

### Analytics Service `/api/analytics`

| Method | Path                          | Auth | Description                        |
|--------|-------------------------------|------|------------------------------------|
| GET    | `/api/analytics/eco-stats`    | —    | Platform-wide eco impact stats     |
| GET    | `/api/analytics/categories`   | —    | Item category activity stats       |
| GET    | `/api/analytics/cities`       | —    | Activity breakdown by city         |

---

## Development Workflow

### Building locally (without Docker)

```bash
# Restore all packages
dotnet restore ResX.sln

# Build the entire solution
dotnet build ResX.sln

# Run a specific service (e.g., Listings)
cd src/Services/Listings/ResX.Listings.API
dotnet run
```

### Running tests

```bash
# All tests
dotnet test ResX.sln

# Tests for a specific service
dotnet test src/Services/Listings/ResX.Listings.Tests/
```

### Database migrations

Each service runs FluentMigrator migrations automatically on startup. To run migrations manually:

```bash
cd src/Services/Listings/ResX.Listings.API
dotnet run -- migrate
```

### Adding a new integration event

1. Define the event class in `ResX.Common/Events/`
2. Publish in the source service via `IEventBus.PublishAsync()`
3. Create a handler in the consuming service implementing `IIntegrationEventHandler<TEvent>`
4. Register the handler in `DependencyInjection.cs` of the consuming service's Infrastructure layer

### Adding a new gRPC endpoint

1. Add the method to the relevant `.proto` file in `src/Protos/`
2. Rebuild the solution (Protobuf code generation runs automatically)
3. Implement the server-side service method in Infrastructure
4. Inject the generated gRPC client in consuming services

### Code conventions

- Nullability enabled everywhere (`<Nullable>enable</Nullable>`)
- Implicit usings enabled
- `System.Text.Json` for all serialization (no Newtonsoft.Json)
- Async/await throughout — no blocking calls (`.Result`, `.Wait()`)
- Domain logic only in Domain layer; no business logic in controllers
- Every aggregate state change must raise domain events

---

## Project Structure

```
ResX/
├── src/
│   ├── BuildingBlocks/
│   │   ├── ResX.Common/               # Shared base classes (AggregateRoot, ValueObject, Result<T>, etc.)
│   │   ├── ResX.EventBus.RabbitMQ/    # RabbitMQ event bus implementation
│   │   ├── ResX.Caching.Redis/        # Redis distributed cache
│   │   └── ResX.Storage.S3/           # S3/Yandex Object Storage client
│   ├── Protos/
│   │   ├── identity.proto
│   │   ├── users.proto
│   │   ├── listings.proto
│   │   └── files.proto
│   ├── Services/
│   │   ├── Analytics/
│   │   │   ├── ResX.Analytics.Application/
│   │   │   ├── ResX.Analytics.Domain/
│   │   │   ├── ResX.Analytics.Infrastructure/
│   │   │   └── ResX.Analytics.API/
│   │   ├── Charity/        (same 4-layer structure)
│   │   ├── Disputes/       (same 4-layer structure)
│   │   ├── Files/          (same 4-layer structure)
│   │   ├── Identity/       (same 4-layer structure)
│   │   ├── Listings/       (same 4-layer structure)
│   │   ├── Messaging/      (same 4-layer structure)
│   │   ├── Notifications/  (same 4-layer structure)
│   │   ├── Transactions/   (same 4-layer structure)
│   │   └── Users/          (same 4-layer structure)
│   └── Gateway/
│       └── ResX.ApiGateway/           # YARP reverse proxy gateway
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
│   └── init-databases.sql             # Creates all PostgreSQL databases on first start
├── docker-compose.yml                 # Full local environment orchestration
├── docker-compose.override.yml        # Dev overrides (debug logging, hot-reload)
└── ResX.sln                           # Visual Studio solution (44 projects)
```

---

## Contributing

1. **Fork** the repository and create a feature branch:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Follow Clean Architecture**: business logic goes in Domain or Application layers, never in Infrastructure or API.

3. **Write tests**: unit tests for domain logic, integration tests for repositories and API endpoints.

4. **Keep migrations additive**: never edit existing FluentMigrator migration classes — always create new ones.

5. **Open a Pull Request** against `main` with a clear description and relevant issue numbers.

### Branch naming

| Type      | Pattern                     |
|-----------|-----------------------------|
| Feature   | `feature/short-description` |
| Bug fix   | `fix/short-description`     |
| Refactor  | `refactor/short-description`|
| Infra     | `infra/short-description`   |

---

## License

This project is proprietary software. All rights reserved.

---

*Built with .NET 10 · PostgreSQL · RabbitMQ · Redis · Kubernetes*
