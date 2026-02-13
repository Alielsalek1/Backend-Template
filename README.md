# 🛡️ The Architect's Forge: Backend Odyssey

Welcome to the **Architect's Forge**, a high-performance, resilient backend template powered by .NET 9 and built with Clean Architecture. 

This project is currently in its **Refactoring Phase**. Having successfully passed the first stage of TDD (Test-Driven Development) — where all integration tests pass with minimal viable code — the focus has shifted to code optimization, structuring, and enhancing system observability.

---

## 🚀 Project Overview

The forge is designed to provide a robust starting point for modern web applications. It implements core features with a heavy emphasis on reliability and idempotency:

*   **🔐 Dual-Layer Authentication:** Integrated support for both internal (Email/Password) and external (Google OAuth2) authentication schemes.
*   **🛡️ Strong Idempotency:** Custom action filters powered by Redis ensure that critical requests (like registration or updates) are resilient to network failures and accidental retries.
*   **👤 User Management:** Full profile management systems including phone and address updates, email verification, and password recovery.
*   **👁️ Total Observability:** Detailed structured logging across all services and middlewares, integrated with Seq for real-time analysis.
*   **🧪 Absolute Integration:** A suite of 58+ integration tests that spin up real infrastructure (Postgres, Redis, RabbitMQ) to guarantee system integrity.

---

## ⚡ Power Grid (Technology Stack)

The Forge utilizes a sophisticated network of modern tools and frameworks:

### **Core Stack**
*   **.NET 9 (C#):** The latest high-performance framework from Microsoft.
*   **Entity Framework Core:** EF Core 9 with Npgsql for PostgreSQL interactions.
*   **PostgreSQL:** The primary relational database for persistent storage.
*   **Redis:** High-speed distributed cache for session management and idempotency locking.
*   **RabbitMQ & MassTransit:** Enterprise-grade service bus for asynchronous event processing.

### **Security & Identity**
*   **JWT Bearer:** Standardized token-based authentication.
*   **Google OAuth2:** External login integration.
*   **BCrypt.Net:** Industrial-strength password hashing.
*   **Asp.Versioning:** Semantic API versioning.

### **Application Logic**
*   **FluentValidation:** Expressive validation for incoming DTOs.
*   **Mapster:** High-performance object mapping.
*   **Serilog:** Structured logging with sinks for Console, File, and Seq.
*   **FluentEmail:** Elegant email composition and delivery through SMTP.

### **Testing & Quality**
*   **xUnit:** Leading .NET testing framework.
*   **Testcontainers:** Docker orchestration during test runs for real-world simulation.
*   **Respawn:** Database cleanup between test scenarios.
*   **MailHog:** Local SMTP testing server for email verification flows.

---

## 🛠️ Leveling Up (Getting Started)

1.  **Summon the Artifacts:**
    ```bash
    docker compose up -d
    ```
2.  **Ignite the Engine:**
    ```bash
    dotnet run --project Src/API
    ```
    Open `MyBackendTemplate.sln` in VS Code or Visual Studio.
3.  **Inspect the Chronicles:**
    Visit `http://localhost:8081` to view your realm's heartbeats in Seq.
4.  **Enter the Admin Chambers:**
    Visit `http://localhost:15672` to manage the RabbitMQ Messenger Guild (guest/guest).

---

## 📜 Epic Chronicles

To dive deeper into the ancient technology used in this forge, read the [**TECHNOLOGIES.md**](./TECHNOLOGIES.md) manuscript.

*May your latencies be low and your uptimes eternal.*
