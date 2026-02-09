# 🧪 The Forbidden Alchemies: Technical Manuscript

This manuscript documents the advanced alchemies and arcane protocols used to build the **Architect's Forge**.

---

## 🏛️ Architecture: The Pillars of Creation

### **Clean Architecture**
The template follows the sacred principles of Clean Architecture. Dependency flow is strictly inward: `API -> Application -> Domain <- Infrastructure`. This ensures that your business logic (the Domain) remains untainted by the shifting sands of external technologies.

### **The Result Pattern**
We avoid the "Throwing the Exception" ritual for expected failures. Instead, we use the **Result Pattern**, returning a standard outcome object that either contains the prize or a detailed description of why the quest failed.

---

## 🌩️ Messaging: The Messenger Guild

### **MassTransit & RabbitMQ**
Asynchronous communication is handled by **MassTransit**, an abstraction layer over **RabbitMQ**. It allows your services to whisper secrets to each other without being tightly coupled.
*   **Consumers:** Dedicated workers that process incoming scrolls (messages).
*   **Publishing:** The act of sending a scroll to the guild for delivery.

---

## 🔒 Defense: Idempotency Protocols

### **The Idempotency-Key Ritual**
To prevent a client from accidentally casting the same spell twice (e.g., double charging a user), we implement **Idempotent Filters**.
1.  A client sends a unique `Idempotency-Key` in the header.
2.  The API checks the **Redis Relic** to see if this key has been used recently.
3.  If found, it returns the cached answer without re-running the logic.
4.  If new, it performs the action and stores the result for future reference.

---

## 📖 Chronicles: The All-Seeing Eye

### **Serilog & Seq**
Logs are not just text; they are **Structured Data**. 
*   **Serilog:** Enrich logs with contextual metadata (Environment, UserID, RequestPath).
*   **Seq:** A powerful UI that filters and searches these logs in real-time. Navigate to `http://localhost:8081` to witness the flow.

---

## 🏗️ The Forge: Dockerization

The entire realm is contained within **Docker Vessels**:
*   **Multistage Builds:** The API Dockerfile optimizes for size by separating the "Build" and "Runtime" phases.
*   **Network Insulation:** All services communicate within a private `backend` network, exposing only the necessary gateways to the host world.

---

## 🧪 Simulation: Testcontainers

We don't trust the "Works on my Machine" curse. 
Using **Testcontainers**, our integration tests spin up real, short-lived Docker containers for PostgreSQL and Redis. This ensures that your tests are running in an environment identical to your production realm.

---

### 📜 Glossary of Artifacts

*   **.NET 9 SDK:** The core engine.
*   **Entity Framework Core:** The portal to the Database.
*   **StackExchange.Redis:** The bridge to the high-speed cache.
*   **DotNetEnv:** The scribe that reads environmental variables.
*   **Asp.Versioning:** Manages different versions of your API API routes.
