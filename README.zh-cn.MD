# OpenProfileServer

[![License: AGPL v3](https://img.shields.io/badge/协议-AGPL_v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Status](https://img.shields.io/badge/状态-积极开发中-green.svg)](https://github.com/XIYUEKONGLING/OpenProfile)

**OpenProfileServer** 是一个高性能、以身份为核心的电子名片与数字个人档案后端系统。基于最新的 .NET 10 构建，它提供独特的**双模式部署策略**：既可以作为功能完备的动态 API 运行，也可以作为静态网站生成器使用。

[🖥️ 前端仓库](https://github.com/XIYUEKONGLING/OpenProfileCard) | [📚 API 规范文档](API.MD) | [🚀 部署与导出教程](DEPOLY.zh-cn.MD)

- [English](README.MD)

---

## 🌟 核心理念：双模式部署

不同于传统的 CMS，OpenProfile 赋予你两全其美的选择：

1.  **动态 CMS 模式**：作为标准 Web API 运行。适用于需要实时更新、用户注册和社交互动（关注/屏蔽）的组织或个人。
2.  **静态 Edge 模式**：使用内置生成器将整个数据库导出为一系列 JSON 文件。可部署至 **GitHub Pages**、**Cloudflare Pages** 或 **Vercel**，实现零成本、全球加速且极速的托管体验。

---

## 🚀 核心特性

### 👤 身份管理
- **多账号支持**：采用 EF Core TPT 继承模式，完美支持**个人**与**组织**档案。
- **丰富的数据结构**：支持工作经历、教育背景、项目作品集、展示柜、社交链接等。
- **组织中心**：基于角色的管理系统（所有者/管理员/成员），配有完整的邀请体系。

### 🛡️ 安全与性能
- **现代认证**：Argon2id 密码哈希，JWT 结合 SecurityStamp 校验（支持“注销所有设备”）。
- **智能缓存**：通过 **FusionCache** 实现多级缓存（L1 内存 + L2 Redis）。
- **速率限制**：针对敏感接口（登录/注册）的智能频率控制。
- **隐私优先**：细粒度的可见性控制与黑名单逻辑。

### 🛠️ 开发者友好
- **自动检测**：配套前端会根据后端是静态还是动态，自动调整 UI 功能（如隐藏登录按钮）。
- **数据库灵活性**：支持 SQLite、PostgreSQL 和 MySQL。

---

## 🏗️ 架构说明

### 1. OpenProfileServer (动态 API)
核心程序。处理业务逻辑、身份验证和数据持久化的 ASP.NET Core 10 Web API。

### 2. OpenProfileServer.Generator (静态生成器)
专用工具。它会扫描数据库并生成模拟 API 结构的静态 JSON 树（例如：`/api/profiles/alice` 变为 `/api/profiles/alice.json`）。

---

## 🛠️ 技术栈

- **运行时**: .NET 10.0
- **ORM**: Entity Framework Core
- **缓存**: FusionCache + Redis
- **安全**: BouncyCastle (Argon2id), JWT
- **邮件**: MailKit (SMTP)
- **文档**: Swagger / OpenAPI 3.0

---

## 📄 开源协议

本项目采用 **[GNU Affero General Public License v3.0 (AGPL-3.0)](LICENSE)** 协议开源。
*注意：如果您修改了代码并将其作为 Web 服务提供，您必须公开您的源代码。*
