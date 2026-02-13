# 🧪 Technical Manuscript

This manuscript documents the alchemies and protocols used to build the **Architect's Forge**.

---

## 🏛️ Architecture

### **Clean Architecture**
The template follows the sacred principles of Clean Architecture. Dependency flow is strictly inward to ensure that your business logic remains untainted by the shifting sands of external technologies.

### **The Result Pattern**
We avoid the "Throwing the Exception" ritual for expected failures. Instead, we use the **Result Pattern**, returning a standard outcome object that either contains the prize or a detailed description of why the quest failed.

---

## 🌩️ Messaging

### **MassTransit & RabbitMQ**
Asynchronous communication is handled by **MassTransit**, an abstraction layer over **RabbitMQ**. It allows your services to communicate without being tightly coupled.

---

## 🔒 Defense: Idempotency Protocols

### **The Idempotency-Key Ritual**
To prevent a client from accidentally casting the same spell twice (e.g., double charging a user), we implement **Idempotent Filters**. 
Critical actions require a unique `Idempotency-Key`. The system checks the **Redis Relic** to see if this key has been used recently, returning cached results for duplicates.

---

## 📖 Chronicles: Structured Logging

### **Serilog & Seq**
Logs are not just text; they are **Structured Data**. 
*   **Serilog:** Enrich logs with contextual metadata (Environment, UserID, RequestPath).
*   **Seq:** A powerful analytics engine that filters and searches these logs in real-time.

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
