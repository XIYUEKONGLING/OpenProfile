# OpenProfileServer

[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Build Status](https://img.shields.io/badge/status-early__development-orange.svg)](https://github.com/yourusername/OpenProfileServer)

**OpenProfileServer** is a backend system designed for electronic business cards and digital identity management. It supports rich data structures including work history, education, social links, and certifications.

The project provides two distinct deployment strategies: a **Dynamic API** for full content management and a **Static Generator** for high-performance, read-only hosting.

## 🚀 Key Features

-   **Multi-Account Support**: Distinct profiles for **Personal**, **Organization**, and **Application** accounts using Entity Framework Core TPT (Table-Per-Type) inheritance.
-   **Rich Profile Data**: Comprehensive support for assets, work experience, education, galleries, projects, and sponsorship links.
-   **Dual Deployment Modes**:
    -   **Dynamic Mode**: Full API with Authentication, Registration, and Admin capabilities.
    -   **Static Mode**: Generates a directory of JSON DTOs mirroring the API structure for deployment on Cloudflare Pages, GitHub Pages, etc.
-   **Smart Frontend Detection**: The frontend automatically detects the backend type (Static vs. Dynamic) and enables/disables features (like Login) accordingly.
-   **Advanced Security**:
    -   **Argon2id** hashing for password storage (via BouncyCastle).
    -   JWT-based authentication with refresh tokens.
    -   Built-in Rate Limiting (Sliding Window, Fixed Window, Token Bucket).
-   **High-Performance Caching**: Integrated **FusionCache** with support for Memory and Redis (L2 Cache + Backplane).
-   **Flexible Database**: Support for SQLite (Development), PostgreSQL, and MySQL.

## 🏗️ Architecture

This repository contains two main programs:

### 1. OpenProfileServer (Dynamic API)
The standard ASP.NET Core Web API.
-   **Purpose**: Content Management System (CMS).
-   **Features**: Handles HTTP requests, Authentication, Database writes, and real-time updates.
-   **Usage**: Used when you need to edit profiles, manage users, or require dynamic interactions.

### 2. OpenProfileServer.Generator (Static Exporter)
A console application tool.
-   **Purpose**: Static Site Generation.
-   **Input**: Connects to the database using `appsettings.json`.
-   **Output**: Generates a complete directory structure of **JSON DTOs** that exactly mirrors the API's response format.
-   **Usage**: Run this after you finish editing your profile. Upload the output to a static host.

### Frontend Behavior
The frontend client performs an automatic check against the backend to determine:
1.  **Is it a Static Pseudo-Backend?** (Checks if responses are served as static files).
2.  **Does it support Dynamic Features?** (Checks for API availability).

Based on these indicators, the frontend UI adapts—hiding login buttons in Static mode or enabling admin panels in Dynamic mode.

## 🛠️ Tech Stack

-   **Framework**: ASP.NET Core 10.0
-   **ORM**: Entity Framework Core
-   **Caching**: FusionCache (ZiggyCreatures)
-   **Security**: BouncyCastle (Cryptography), System.IdentityModel.Tokens.Jwt
-   **API Documentation**: Swagger (OpenAPI)

## 📋 Prerequisites

-   [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
-   Docker (optional, for containerized deployment)

## ⚙️ Configuration

Both the Server and the Generator rely on `appsettings.json` for configuration.

-   **DatabaseSettings**: Configure the connection string and provider (SQLite, PgSQL, MySQL).
-   **Security**: Define the Root account credentials and JWT signing secrets.
-   **Cache**: Toggle Redis support and set expiration policies.
-   **RateLimiting**: Define custom throttling policies for different endpoints.

*Note: Never commit secrets to version control. Use `appsettings.Development.json` for local overrides.*

## 🏃 Usage

### Option A: Running the Dynamic Server

Use this mode to manage data, register accounts, and edit profiles.

1.  **Run the Server**
    ```bash
    dotnet run --project OpenProfileServer
    ```

2.  **Access API Documentation**
    Navigate to `https://localhost:5001/swagger` to interact with the API.

### Option B: Generating Static Site

Use this mode to export your data for static hosting.

1.  **Ensure Data is Ready**
    Make sure you have run the Dynamic Server and populated your profiles in the database.

2.  **Run the Generator**
    The generator uses the same `appsettings.json` to read the database.
    ```bash
    dotnet run --project OpenProfileServer.Generator -- --output ./static-export
    ```

3.  **Deploy**
    Upload the contents of `./static-export` to Cloudflare Pages, GitHub Pages, or any static web host. The directory structure will mimic the API routes (e.g., `/api/profiles/{id}.json`).

## 🐳 Docker Support

A Dockerfile is included for the Dynamic Server.

```bash
docker build -t openprofile-server .
docker run -p 8080:8080 openprofile-server
```

## 📊 Development Status

**Current Phase: Early Development (Alpha)**

The project is currently in the initial design and implementation phase. The core data models, authentication layer, and database context are established.

### Roadmap

-   [x] Core Entity Models (Account, Profile, Settings...)
-   [ ] Complete implementation of Dynamic API Controllers
-   [ ] **Implementation of Static Generator (Export Logic)**
-   [ ] Frontend Client

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## 📄 License

This project is licensed under the **GNU Affero General Public License v3.0 (AGPL-3.0)**.

See the [LICENSE](LICENSE) file for details.
