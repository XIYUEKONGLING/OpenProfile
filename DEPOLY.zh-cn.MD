# 🌐 部署与静态导出指南

本指南旨在帮助非 .NET 开发背景的用户快速搭建 **动态服务器** 或执行 **静态导出**。

## 📋 准备工作

在开始之前，您必须在服务器或本地计算机上安装 **.NET 10.0 SDK**。
- [下载 .NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)

---

## 🚀 第一部分：运行动态服务器 (API)

动态服务器是项目的中枢，负责处理登录、档案编辑和数据库操作。

### 1. 配置服务器
找到 `OpenProfileServer` 文件夹下的 `appsettings.json`。使用文本编辑器打开并更新以下**关键**设置：

*   **Security (安全)**：
    *   `RootPassword`: 设置初始管理员账号的强密码。
    *   `ApplicationSecret`: **至关重要。** 修改为一个随机的、至少 32 位的字符串。
*   **DatabaseSettings (数据库)**：
    *   `Type`: 选择 `SQLite` (最简单)、`PgSQL` (PostgreSQL) 或 `MySQL`。
    *   `ConnectionString`: 根据您的数据库地址进行更新。
*   **Cors (跨域)**：
    *   `AllowedOrigins`: 填入您的前端 URL（例如：`["https://card.example.com"]`），否则浏览器会拦截请求。

### 2. 启动程序
在项目根目录打开终端（命令行），运行：

```bash
dotnet run --project OpenProfileServer
```
服务器将启动（默认通常在 `http://localhost:5071`）。首次运行时，它会自动创建数据库并初始化表结构。

---

## 🏗️ 第二部分：生产环境与集群注意事项

如果您从本地测试环境迁移到**生产环境**，请务必遵守以下规则：

1.  **禁止使用默认值**：确保 `ApplicationSecret` 和 `RootPassword` 是唯一的。
2.  **数据库选择**：对于高并发场景，请使用 **PostgreSQL** 或 **MySQL** 而非 SQLite。
3.  **集群/多实例部署**：
    *   如果您在负载均衡器后运行多个服务器实例，**必须**启用 Redis。
    *   在 `appsettings.json` 中，将 `Cache:UseRedis` 设置为 `true` 并提供 `RedisConnection` 地址。
    *   *原因*：这能确保所有实例共享用户会话和安全密钥（Data Protection）。
4.  **HTTPS**：务必在 SSL 证书后运行 API（使用 Nginx、Caddy 或 Cloudflare 反向代理）。

---

## 📦 第三部分：静态导出教程

静态生成器会将您的数据“拍照”并保存为 JSON 文件。您可以将这些文件上传到静态托管平台（GitHub Pages、Cloudflare Pages），这样无需运行后端，前端也能正常工作。

### ⚠️ 重要提示：开始前必读
*   **停止服务器**：确保动态服务器**没有在运行**。
*   **SQLite 用户注意**：如果您使用 SQLite，必须关闭所有正在操作该数据库的程序。SQLite 在运行时会产生临时文件（如 `-wal` 或 `-shm`）。如果在数据库未关闭时复制，导出的数据可能会损坏或不完整。

### 导出步骤：

1.  **同步配置文件**：
    将您在 `OpenProfileServer` 文件夹中修改好的 `appsettings.json` 复制到 `OpenProfileServer.Generator` 文件夹下。
2.  **复制数据库（仅限 SQLite）**：
    如果您使用 SQLite，请将 `.db` 数据库文件也复制到 `OpenProfileServer.Generator` 文件夹下。
3.  **运行导出器**：
    在终端运行：
    ```bash
    dotnet run --project OpenProfileServer.Generator -- --output ./my-static-site
    ```
4.  **检查结果**：
    程序会生成一个 `my-static-site` 文件夹，里面包含一个 `api` 目录，充满了 JSON 文件。
5.  **部署**：
    将 `my-static-site` 文件夹内的内容上传到静态空间。将前端的 `VITE_API_BASE_URL` 指向该空间的 URL 即可。

---

## 🛠️ 故障排除

*   **"Configuration Validation Failed"**：这表示您的 `appsettings.json` 缺少必要设置（如密钥太短或数据库类型写错）。请查看控制台输出的具体错误提示。
*   **数据库被锁定 (Database Locked)**：确保导出时没有任何数据库工具或服务器实例正在占用 SQLite 的 `.db` 文件。
*   **跨域错误 (CORS Error)**：如果前端无法获取数据，请检查 API 配置中的 `Cors:AllowedOrigins` 是否精确匹配了前端的域名。
