# OpenProfileServer API Specification (ALPHA)

## 1. Overview

### 1.1 Identifiers
All endpoints accepting a profile or organization identifier support two formats:
- **AccountName**: `username` (e.g., `/api/profiles/alice`)
- **UUID**: `@{guid}` (e.g., `/api/profiles/@550e8400-e29b-41d4-a716-446655440000`)

### 1.2 Data Format
- **Casing**: The API uses **PascalCase** for all JSON keys in both requests and responses.
- **Input Flexibility**: JSON property matching is case-insensitive for incoming requests to improve client compatibility.

### 1.3 Status & Visibility Logic

The API response and Static Generator behavior depend on the `AccountStatus`.

| Status | API Response | Static Generator | Account Deletion | Social Actions |
| :--- | :--- | :--- | :--- | :--- |
| **Active** | Full Data | Full Generation | ✅ Allowed | ✅ Allowed |
| **Suspended** | Restricted (Basic Info Only) | Restricted (No sub-resources) | ✅ **Allowed** | ❌ Forbidden |
| **Banned** | Banned (Status Only) | Banned (Status Only) | ❌ **Forbidden (Locked)** | ❌ Forbidden |
| **PendingDeletion**| 404 / Error | Skipped | N/A (Must Restore) | ❌ Forbidden |

### 1.4 Update Semantics

- **POST**: Full Update (Replace all fields). Missing fields will be reset to defaults.
- **PATCH**: Partial Update (Update only provided non-null fields).

---

## 2. Server & Meta (Public)

These endpoints provide information about the server instance, enabled features, and site branding.

| Method | Endpoint | Description | Static File |
| :--- | :--- | :--- | :--- |
| **GET** | `/api` | Get combined server info, features, and site metadata. | `api/index.json` |
| **GET** | `/api/info` | Get standalone server version and mode. | `api/info.json` |
| **GET** | `/api/meta` | Get standalone site branding metadata. | `api/meta.json` |
| **GET** | `/api/features` | **NEW** Get server feature flags (Email enabled, Registration allowed, etc.). | `api/features.json` |

### 2.1 Admin Management (Protected)

Manage site branding (Site Name, Description, Logo, etc.).

| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **POST** | `/api/admin/meta` | **Full Update** site metadata. | AdminOrHigher |
| **PATCH** | `/api/admin/meta` | **Partial Update** site metadata. | AdminOrHigher |

---

## 3. Authentication (Dynamic Only)

Authentication and Registration flow with mandatory email verification support.

### 3.1 Flow Logic
1.  **Check Configuration**: Call `GET /api/features` (or auth config) to check `EmailVerification`.
2.  **Request Code (If required)**: If `true`, call `POST /api/auth/send-code` with `{ email, type: 0 }`.
3.  **Register**: Call `POST /api/auth/register` including the `code`. The account is created as **Verified** and **Logged In** atomically.

| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/auth/config` | Get auth settings (e.g. is verification required?). | No |
| **POST** | `/api/auth/send-code` | Send verification code to email. (Type: 0=Register, 3=VerifyEmail) | No |
| **POST** | `/api/auth/register` | Create account. Requires `code` if configured. Returns JWT. | No |
| **POST** | `/api/auth/login` | Login. Returns JWT + RefreshToken. | No |
| **POST** | `/api/auth/refresh` | Exchange RefreshToken for a new JWT. | No |
| **POST** | `/api/auth/logout` | Revoke a specific RefreshToken. | Yes |
| **POST** | `/api/auth/logout-all` | **SecurityStamp Reset**: Revokes ALL sessions and JWTs. | Yes |


*Note: Access Tokens are validated against the database's `SecurityStamp` on every request. A reset will result in immediate 401 Unauthorized for all existing JWTs.*

---

## 4. Business Logic Details (Block & Follow)

### 4.1 Block Priority
When User A attempts to Follow User B, the system checks:
1.  Does A Block B? → **Yes**: Follow Forbidden.
2.  Does B Block A? → **Yes**: Follow Forbidden (Return 403 or 404).

### 4.2 Block Side Effects
When User A calls `POST .../block` to block User B:
1.  If A is Following B → **Auto Unfollow**.
2.  If B is Following A → **Auto Remove Follower** (B no longer follows A).

### 4.3 Static Generation Privacy
- **Status Filtering**: When generating `followers.json` or `following.json`, users with `Status != Active` must be filtered out.
- **Privacy Settings**:
  - If `AccountSettings.ShowFollowersList == false`, the generator should produce an empty array `[]` (instead of 404) to maintain frontend consistency.
  - The same logic applies to `following.json`.

---

## 5. Public Profiles (Read-Only / Static Compatible)

These endpoints are used by the Static Generator.

| Method | Endpoint | Description | Static File |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/profiles/{profile}` | Get profile info. Content depends on Status. | `api/profiles/{profile}.json` |
| **GET** | `/api/profiles/{profile}/{resource}` | Get sub-resources (projects, gallery, etc.). | `api/profiles/{profile}/{resource}.json` |
| **GET** | `/api/profiles/{profile}/memberships` | Get public organizations this user belongs to. | `api/profiles/{profile}/memberships.json` |
| **GET** | `/api/profiles/{profile}/members` | Get **public** members (Org only). | `api/profiles/{profile}/members.json` |
| **GET** | `/api/profiles/{profile}/followers` | Get followers list. | `api/profiles/{profile}/followers.json` |
| **GET** | `/api/profiles/{profile}/following` | Get following list. | `api/profiles/{profile}/following.json` |

*Resources: `projects`, `gallery`, `socials`, `certificates`, `sponsorships`, `work`, `education`*

---

## 6. Personal Management (Dynamic / Protected)

### 6.1 Lifecycle
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **DELETE** | `/api/me` | **Request Account Deletion** (Cooling-off). Forbidden if Banned. | Owner |
| **POST** | `/api/me/restore` | **Restore Account** (Cancel deletion). | Owner |

### 6.2 Account & Security
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/me` | Get full account details. | Owner |
| **GET** | `/api/me/permissions` | **Get current Account's Role and Type**. | Owner |
| **GET** | `/api/me/emails` | List all emails. | Owner |
| **POST** | `/api/me/emails` | Add new email. | Owner |
| **POST** | `/api/me/emails/{email}/primary` | Set as primary email. | Owner |
| **POST** | `/api/me/emails/{email}/verify` | Verify email with code. | Owner |
| **DELETE** | `/api/me/emails/{email}` | Remove email. | Owner |
| **POST** | `/api/me/password` | Change password. | Owner |
| **GET** | `/api/me/blocks` | **List blocked users**. | Owner |
| **GET** | `/api/me/follow-stats` | Get follow counts. | Owner |

### 6.3 Settings & Profile
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/me/settings` | Get personal settings. | Owner |
| **POST** | `/api/me/settings` | Full update settings. | Owner |
| **PATCH** | `/api/me/settings` | Partial update settings. | Owner |
| **GET** | `/api/me/profile` | Get profile (Edit View). | Owner |
| **POST** | `/api/me/profile` | Full update profile. | Owner |
| **PATCH** | `/api/me/profile` | Partial update profile. | Owner |

### 6.4 Invitations (Incoming)
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/me/invitations` | List received invitations. | Owner |
| **POST** | `/api/me/invitations/{id}/accept` | Accept invitation. | Owner |
| **POST** | `/api/me/invitations/{id}/decline` | Decline invitation. | Owner |

### 6.5 Social Actions
*Operator must be in **Active** status.*

| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **POST** | `/api/profiles/{profile}/follow` | **Follow** target user/org. | User (Active) |
| **DELETE** | `/api/profiles/{profile}/follow` | **Unfollow**. | User (Active) |
| **GET** | `/api/profiles/{profile}/follow` | Check if I am following this user. | User |
| **POST** | `/api/profiles/{profile}/block` | **Block** target (Auto unfollow & remove follower). | User (Active) |
| **DELETE** | `/api/profiles/{profile}/block` | **Unblock** target. | User (Active) |

### 6.6 Sub-Resources
*Endpoints: `/api/me/{resource}` and `/api/me/{resource}/{id}`*
*Methods: GET, POST (Create/Update), PATCH (Partial Update), DELETE.*

### 6.7 Notifications (Inbox)

Independent of email system. Used for in-app alerts and announcements.

| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/me/notifications` | List notifications. Supports `?page=1&pageSize=20&unreadOnly=true`. | Owner |
| **GET** | `/api/me/notifications/unread-count` | Get total count of unread messages. | Owner |
| **PATCH** | `/api/me/notifications/{id}/read` | Mark a single notification as read. | Owner |
| **POST** | `/api/me/notifications/read-all` | Mark all notifications as read. | Owner |
| **DELETE** | `/api/me/notifications/{id}` | Delete a specific notification. | Owner |
| **DELETE** | `/api/me/notifications` | Delete all read notifications (Cleanup). | Owner |

---

## 7. Organization Management (Dynamic / Protected)

Standard logic for users and owners. Admin overrides are handled in Section 8.

### 7.1 Lifecycle
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs` | List my organizations. | User |
| **POST** | `/api/orgs` | Create new organization. | User |
| **DELETE** | `/api/orgs/{org}` | **Request Dissolution** (Forbidden if Banned). | **Owner** |
| **POST** | `/api/orgs/{org}/restore` | **Restore Organization** (Cancel dissolution). | **Owner** |

### 7.2 Settings & Profile
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}` | Org Dashboard. | Member+ |
| **GET** | `/api/orgs/{org}/settings` | Get settings. | **Owner** |
| **POST** | `/api/orgs/{org}/settings` | Full update settings. | **Owner** |
| **PATCH** | `/api/orgs/{org}/settings` | Partial update settings. | **Owner** |
| **POST** | `/api/orgs/{org}/profile` | Full update profile. | **Admin+** |
| **PATCH** | `/api/orgs/{org}/profile` | Partial update profile. | **Admin+** |
| **GET** | `.../follow-stats` | Get org follow counts. | Member+ |

### 7.3 Member Management
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}/members` | List all members. | Member+ |
| **GET** | `/api/orgs/{org}/members/me/role` | **Get my Role in this organization**. | Member+ |
| **POST** | `/api/orgs/{org}/members` | Invite/Add member. | **Admin+** |
| **PATCH** | `/api/orgs/{org}/members/me` | Update my visibility/title. | Member+ |
| **DELETE** | `/api/orgs/{org}/members/me` | Leave organization. | Member+ |
| **PATCH** | `/api/orgs/{org}/members/{user}` | Update member role. | **Owner** |
| **DELETE** | `/api/orgs/{org}/members/{user}` | Kick member. | **Owner** |

### 7.4 Invitations (Outgoing)
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}/invitations` | List pending invitations. | **Admin+** |
| **POST** | `/api/orgs/{org}/invitations` | Send invitation. | **Admin+** |
| **DELETE** | `/api/orgs/{org}/invitations/{id}` | Revoke invitation. | **Admin+** |

### 7.5 Sub-Resources
*Endpoints: `/api/orgs/{org}/{resource}`*
*Permission: **Admin+***

---

## 8. System Administration (Dynamic / Protected)

Admins can perform operations that bypass standard checks (e.g., inviting users without acceptance, kicking owners) and manage global configuration.

### 8.1 User & Account Management
| Method | Endpoint | Description | Permission | Query Params |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/admin/users` | List users with pagination. | Admin+ | `page`, `pageSize`, `search`, `status`, `role` |
| **POST** | `/api/admin/users` | Force create user/org. **If creating an Org, it is created EMPTY (no members).** | Admin+ | |
| **PATCH** | `/api/admin/users/{user}/status` | Change status (Active/Suspended/Banned). | Admin+ | |
| **DELETE** | `/api/admin/users/{user}` | **Physical Delete** (No cooling-off). | **Root**/Admin | |
| **POST** | `/api/admin/users/{user}/role` | Promote/Demote system role. | **Root** | |

### 8.2 Organization Administration (Force Actions)
These endpoints allow Admins to manage organization members directly, bypassing invitations and standard role restrictions.

| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/admin/orgs/{org}/members` | **List Members** (Visible even if Org/Members are private). | Admin+ |
| **POST** | `/api/admin/orgs/{org}/members` | **Force Add Member**. Adds user immediately (Bypasses invitation). | Admin+ |
| **PATCH** | `/api/admin/orgs/{org}/members/{user}` | **Force Update Member**. Change Role/Title (Can promote to Owner). | Admin+ |
| **DELETE** | `/api/admin/orgs/{org}/members/{user}` | **Force Kick Member**. Can kick anyone including Owners. | Admin+ |

### 8.3 System Configuration (Runtime)
Manage dynamic system keys (e.g., `AllowRegistration`, `MaintenanceMode`, `Email:Template:*`).

| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/admin/system-settings` | List all system configuration keys. | Admin+ |
| **GET** | `/api/admin/system-settings/{key}` | Get specific setting value. | Admin+ |
| **PUT** | `/api/admin/system-settings/{key}` | **Update** specific setting value. | Admin+ |
