# RestaurantOS v1.0

Production-grade Restaurant ERP desktop application built with **.NET 8 WPF**, **Clean Architecture**, **EF Core 8**, and **SQL Server 2022 (Docker)**.

## Prerequisites

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Quick Start

### Option 1: Startup.bat (recommended)

```bat
dotnet build RestaurantOS.WPF -c Release
Startup.bat
```

### Option 2: Manual

```powershell
# Start SQL Server
docker-compose up -d

# Build & run
dotnet build
dotnet run --project RestaurantOS.WPF
```

On first launch, EF Core migrations and seed data are applied automatically.

## Default Login

| User    | Password    | Role    |
|---------|-------------|---------|
| admin   | admin123    | Admin   |
| ahmet   | waiter123   | Waiter  |
| ayse    | cashier123  | Cashier |

## Architecture

```
RestaurantOS/
├── RestaurantOS.Domain/         # Entities, Enums, Interfaces
├── RestaurantOS.Application/    # DTOs, Service Interfaces, Business Rules
├── RestaurantOS.Infrastructure/ # EF Core, Repositories, Service Implementations
├── RestaurantOS.WPF/            # WPF UI (MVVM), Custom Controls, Animations
├── docker-compose.yml
├── appsettings.json
└── Startup.bat
```

## Features (V1)

- Login with BCrypt password hashing & remembered username
- Dashboard with KPI cards, LiveCharts revenue chart, recent transactions
- Floor Plan with live table status, session management, payments
- Order Kanban board (Pending → Preparing → Served)
- Menu management with categories and availability toggle
- Reports with Excel export (ClosedXML)
- Settings: restaurant config, tables, users, theme accent color
- Custom dark theme design system
- Page transitions, toast notifications, animated counters
- Optimistic concurrency (RowVersion) on sessions/order items

## Database

- **Server:** `localhost,14330` (Docker maps host 14330 → container 1433; avoids conflict with local SQL Server on 1433)
- **Database:** `RestaurantOS`
- **SA Password:** `RestaurantOS@2024!` (change in production)

### Migrations

```powershell
dotnet ef migrations add <Name> --project RestaurantOS.Infrastructure
dotnet ef database update --project RestaurantOS.Infrastructure
```

## Seed Data

- 1 Admin, 1 Waiter, 1 Cashier
- 4 menu categories, 12 sample items
- 20 tables across İç Salon, Bahçe, Bar

## V2 Roadmap

- POS hardware integration
- Kitchen display system
- Multi-branch support
