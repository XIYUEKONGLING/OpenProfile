# OpenProfileServer

[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Status](https://img.shields.io/badge/Status-Active__Development-green.svg)](https://github.com/XIYUEKONGLING/OpenProfile)

**OpenProfileServer** is a high-performance, identity-first backend system for digital business cards and professional profiles. Built with .NET 10, it offers a unique **Dual-Deployment Strategy**: use it as a full-featured Dynamic API or as a Static Site Generator.

[Frontend Repository](https://github.com/XIYUEKONGLING/OpenProfileCard) | [API Specification](API.MD)

---

## 🌟 Core Philosophy: Dual-Mode Deployment

Unlike traditional CMS, OpenProfile gives you the best of both worlds:

1.  **Dynamic CMS Mode**: Run as a standard Web API. Ideal for organizations and individuals who need real-time updates, user registration, and social interactions (Follow/Block).
2.  **Static Edge Mode**: Use the built-in Generator to export the entire database into a directory of JSON files. Deploy to **GitHub Pages**, **Cloudflare Pages**, or **Vercel** for 0-cost, global, and ultra-fast hosting.

---

## 🚀 Key Features

### 👤 Identity Management
- **Multi-Account Support**: TPT inheritance for **Personal** and **Organization** profiles.
- **Rich Data Structures**: Work history, education, project portfolios, galleries, and social links.
- **Organization Hub**: Role-based management (Owner/Admin/Member) with a full invitation system.

### 🛡️ Security & Performance
- **Modern Auth**: Argon2id password hashing and JWT with SecurityStamp validation (Logout all devices).
- **Smart Caching**: Multi-level caching via **FusionCache** (L1 Memory + L2 Redis).
- **Rate Limiting**: Intelligent throttling for sensitive endpoints (Login/Register).
- **Privacy First**: Granular visibility controls and block-list logic.

### 🛠️ Developer Friendly
- **Auto-Detection**: The companion frontend automatically adjusts UI features based on whether the backend is Static or Dynamic.
- **Database Flexibility**: Support for SQLite, PostgreSQL, and MySQL via EF Core.

---

## 🏗️ Architecture

### 1. OpenProfileServer (The API)
The heart of the system. An ASP.NET Core 10 Web API handling logic, authentication, and data persistence.

### 2. OpenProfileServer.Generator (The Exporter)
A specialized tool that crawls the database and generates a static JSON tree that mimics the API structure (e.g., `/api/profiles/alice` becomes `/api/profiles/alice.json`).

---

## 🛠️ Tech Stack

- **Runtime**: .NET 10.0
- **ORM**: Entity Framework Core
- **Cache**: FusionCache + Redis
- **Security**: BouncyCastle (Argon2id), JWT
- **Email**: MailKit (SMTP)
- **Docs**: Swagger / OpenAPI 3.0

---

## 🚦 Quick Start

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Step 1: Configuration
Update `appsettings.json` with your database connection string and security keys.

### Step 2: Run Dynamic Server
```bash
dotnet run --project OpenProfileServer
```

### Step 3: Generate Static Files
```bash
dotnet run --project OpenProfileServer.Generator -- --output ./dist-static
```

---

## 📄 License

This project is licensed under the **[GNU Affero General Public License v3.0 (AGPL-3.0)](LICENSE)**.
*Note: If you modify the code and host it as a service, you must share your changes.*

