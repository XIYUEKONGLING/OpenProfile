# 🌐 Deployment & Static Export Guide

## 📋 Prerequisites

Before starting, you must install the **.NET 10.0 SDK** on your server or local machine.
- [Download .NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)

---

## 🚀 Part 1: Running the Dynamic Server (API)

The Dynamic Server is the "brain" of the project. It handles logins, profile editing, and the database.

### 1. Configure the Server
Locate the `appsettings.json` file in the `OpenProfileServer` folder. Open it with any text editor and update the following **critical** settings:

*   **Security**:
    *   `RootPassword`: Set a strong password for the initial admin account.
    *   `ApplicationSecret`: **Crucial.** Change this to a random string of at least 32 characters.
*   **DatabaseSettings**:
    *   `Type`: Choose `SQLite` (easiest), `PgSQL` (PostgreSQL), or `MySQL`.
    *   `ConnectionString`: Update this to point to your database file or server.
*   **Cors**:
    *   `AllowedOrigins`: Add your frontend URL (e.g., `["https://card.example.com"]`) to allow the browser to talk to the API.

### 2. Run the Program
Open a terminal (Command Prompt or PowerShell) in the root directory of the project and run:

```bash
dotnet run --project OpenProfileServer
```
The server will start (usually on `http://localhost:5071`). On the first run, it will automatically create the database and set up the tables.

---

## 🏗️ Part 2: Production & Cluster Notes

If you are moving from a local test to a **Production Environment**, keep these rules in mind:

1.  **Never Use Defaults**: Ensure `ApplicationSecret` and `RootPassword` are unique.
2.  **Database Choice**: For high traffic, use **PostgreSQL** or **MySQL** instead of SQLite.
3.  **Cluster / Multi-Instance Deployment**:
    *   If you run more than one instance of the server (e.g., behind a Load Balancer), you **MUST** enable Redis.
    *   In `appsettings.json`, set `Cache:UseRedis` to `true` and provide a `RedisConnection`.
    *   *Reason*: This ensures all instances share the same user sessions and security keys (Data Protection).
4.  **HTTPS**: Always run the API behind an SSL certificate (using Nginx, Caddy, or Cloudflare).

---

## 📦 Part 3: Static Export Tutorial

The Static Generator creates a "snapshot" of your data as JSON files. You can upload these files to a static host (GitHub Pages, Cloudflare Pages) so the frontend works without a running backend.

### ⚠️ Important: Before You Start
*   **Stop the Server**: Ensure the Dynamic Server is **not running**.
*   **SQLite Users**: If you use SQLite, closing the program is mandatory. SQLite often creates temporary files (ending in `-wal` or `-shm`) while running. If you copy the database while it's "open," the data might be corrupted or incomplete.

### Step-by-Step Export:

1.  **Sync Configuration**:
    Copy your modified `appsettings.json` from the `OpenProfileServer` folder into the `OpenProfileServer.Generator` folder. The generator needs to know how to talk to your database.
2.  **Copy Database (If using SQLite)**:
    If you used SQLite, copy your `.db` file (e.g., `openprofile.db`) into the `OpenProfileServer.Generator` folder.
3.  **Run the Generator**:
    Open your terminal and run:
    ```bash
    dotnet run --project OpenProfileServer.Generator -- --output ./my-static-site
    ```
4.  **The Result**:
    A new folder named `my-static-site` will appear. It contains an `api` folder filled with JSON files.
5.  **Deployment**:
    Upload the contents of `my-static-site` to your static web host. Point your Frontend's `VITE_API_BASE_URL` to this static URL.

---

## 🛠️ Troubleshooting

*   **"Configuration Validation Failed"**: This means you forgot a required setting in `appsettings.json` (like a short secret key or missing database type). Check the terminal output; it will tell you exactly what is wrong.
*   **Database Locked**: Ensure no other programs (like a database browser or the server) are accessing the SQLite `.db` file during export.
*   **CORS Errors**: If the frontend can't fetch data, double-check that your frontend URL is exactly matched in the `Cors:AllowedOrigins` section of the API config.
