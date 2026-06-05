# Multi-Vendor E-Commerce Platform

A production-ready backend service for a multi-vendor e-commerce platform where merchants can manage products and flexible product variants with unlimited dynamic attributes.

Built with **ASP.NET Core / .NET 9** following **Simplified Clean Architecture**.

---

## Quick Navigation

- [Live Demo](#live-demo)
- [Technology Stack](#technology-stack)
- [Architecture Overview](#architecture-overview)
- [Database Design](#database-design)
- [Project Structure](#project-structure)
- [Setup Instructions](#setup-instructions)
- [Environment Configuration](#environment-configuration)
- [Running Tests](#running-tests)
- [CI/CD Pipeline](#cicd-pipeline)

---

## Live Demo

The API is deployed and fully accessible — no installation required.

👉 **[https://e-commerce-backend.runasp.net/index.html](https://e-commerce-backend.runasp.net/index.html)**

### Seeded Credentials

The database is pre-seeded with two merchant accounts for testing:

| Merchant | Email | Password |
|---|---|---|
| Merchant 1 | `alex@shop.com` | `Password123!` |
| Merchant 2 | `sara@boutique.com` | `Password123!` |

To authenticate: call `POST /api/auth/login` → copy the `accessToken` → click **Authorize** in Swagger UI → paste the token.

---

## Technology Stack

| Component | Technology |
|---|---|
| Framework | .NET 9 / ASP.NET Core |
| ORM | Entity Framework Core 9 |
| Database | SQL Server 2022 |
| Authentication | JWT Bearer + Refresh Tokens |
| Caching | Redis (StackExchange.Redis) |
| Logging | Serilog (Console + Rolling File) |
| Documentation | Swagger / OpenAPI (Swashbuckle 6.9.0) |
| Password Hashing | BCrypt.Net |
| Unit Testing | xUnit + Moq + FluentAssertions |
| Integration Testing | xUnit + WebApplicationFactory |
| CI/CD | GitHub Actions |
| Containerization | Docker + Docker Compose |

---

## Architecture Overview

The solution follows **Simplified Clean Architecture** with four projects and strict one-direction dependency rules:

```
Api  →  Application  →  Domain
              ↑
       Infrastructure
```

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, enums, domain events — no external dependencies |
| **Application** | DTOs, service interfaces, business logic, use cases |
| **Infrastructure** | EF Core DbContext, repositories, auth services, Redis, email |
| **Api** | Controllers, middleware, Swagger config, Program.cs |

**Key principle:** Controllers are thin — they call a service and return a result. All business logic and ownership checks live in the service layer.

---

## Database Design

### ERD & Mapping Document

Full database documentation is in the `/docs` folder:

- 📊 [ERD Diagram](docs/ecommerce-database-erd.png)
- 📄 [Database Mapping Document & System Design Justification](docs/Database%20Mapping%20Document%20%26%20System%20Design%20Justification.pdf)

The mapping document covers: all tables and columns, entity relationships, index strategy, variant system design justification, scalability considerations, and trade-offs.

### Variant System Design

The platform uses a relational **EAV (Entity-Attribute-Value)** pattern to support unlimited product attributes and variant combinations without any hardcoded columns or schema changes.

```
ProductAttribute  →  AttributeOption  →  VariantAttributeValue  ←  ProductVariant
  ("Color")            ("Red", "Blue")        (pivot table)
```

See the mapping document for the full design justification and trade-off analysis.

---

## Project Structure

```
multi-vendor-ecommerce-platform/
├── .github/workflows/ci.yml        ← GitHub Actions CI/CD pipeline
├── docs/                           ← ERD and mapping document
├── src/
│   ├── Api/                        ← Controllers, middleware, Program.cs
│   ├── Application/                ← DTOs, services, interfaces, events
│   ├── Domain/                     ← Entities, enums, base classes
│   └── Infrastructure/             ← DbContext, repositories, auth, caching
├── tests/
│   ├── Application.Tests/          ← Unit tests (xUnit + Moq)
│   └── Integration.Tests/          ← Integration tests (WebApplicationFactory)
├── docker-compose.yml
├── Dockerfile
└── README.md
```

---

## Setup Instructions

### Option 1 — Docker (Recommended)

Starts the API, SQL Server, and Redis in one command.

**1. Create a `.env` file in the project root:**
```env
JWT_KEY=your-super-secret-key-minimum-32-characters-long
```

**2. Start all services:**
```bash
docker compose up --build -d
```

**3. Open Swagger UI:**

Navigate to `http://localhost:8080`

---

### Option 2 — Local (.NET CLI)

Requires a running SQL Server and Redis instance.

**1. Clone the repository:**
```bash
git clone https://github.com/A-Qassem/multi-vendor-ecommerce-platform.git
cd multi-vendor-ecommerce-platform
```

**2. Set secrets via .NET User Secrets:**
```bash
dotnet user-secrets init --project src/Api
dotnet user-secrets set "Jwt:Key" "your-super-secret-key-minimum-32-characters" --project src/Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=MultiVendorEcommerce;User Id=sa;Password=YourPassword;Encrypt=True;TrustServerCertificate=True;" --project src/Api
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379" --project src/Api
```

**3. Run the application:**
```bash
dotnet run --project src/Api
```

**4. Open Swagger UI:**

Navigate to `http://localhost:5222`

The application automatically runs EF Core migrations and seeds test data on first startup.

---

## Environment Configuration

`appsettings.json` contains only empty placeholders — no real secrets are committed to the repository.

| Variable | Description |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `ConnectionStrings:Redis` | Redis connection string |
| `Jwt:Key` | JWT signing secret (minimum 32 characters) |
| `Jwt:Issuer` | JWT issuer claim |
| `Jwt:Audience` | JWT audience claim |
| `Jwt:ExpiresInMinutes` | Access token lifetime (default: 15 minutes) |

> **Security note:** JWT secret was rotated after an accidental early commit. Secrets are managed via .NET User Secrets in development and environment variables in production.

---

## Running Tests

### Unit Tests
```bash
dotnet test tests/Application.Tests
```
Covers `AuthService`, `ProductService`, and `VariantService` — including ownership checks, not found cases, duplicate SKU, and dynamic attribute handling.

### Integration Tests
```bash
dotnet test tests/Integration.Tests
```
33 tests covering the full HTTP stack via `WebApplicationFactory` with an in-memory database. No external dependencies required.

### All Tests
```bash
dotnet test
```

---

## CI/CD Pipeline

GitHub Actions runs automatically on every push and pull request to `main`:

1. Restore → Build → Unit Tests → Integration Tests
2. Docker image build verification

---